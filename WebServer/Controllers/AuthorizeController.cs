using System.Web;
using FileFlows.Server;
using FileFlows.ServerShared.Services;
using FileFlows.WebServer.Helpers;

namespace FileFlows.WebServer.Controllers;

/// <summary>
/// Authorization controller
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public class AuthorizeController : Controller
{
    /// <summary>
    /// The claim used in the verification code
    /// </summary>
    private const string Claim = "emailAddress";

    /// <summary>
    /// Login page/route
    /// </summary>
    /// <param name="message">Optional message to show</param>
    /// <returns>the login page/route</returns>
    [HttpGet("login")]
    public async Task<IActionResult> LoginPage([FromQuery] string? message = null)
    {
        var mode = AuthenticationHelper.GetSecurityMode();
        if (mode == SecurityMode.Off)
        {
            #if(DEBUG)
            return Redirect("http://localhost:5276");
            #else
            return Redirect("/");
            #endif
        }

        var service = ServiceLoader.Load<ISettingsService>();
        if (mode == SecurityMode.OpenIdConnect)
            return RedirectToAction(nameof(OpenIDController.Login), nameof(OpenIDController)[..^10]);
        #if(DEBUG)
        ViewBag.UrlPrefix = "http://localhost:5276/";
        #else
        ViewBag.UrlPrefix = string.Empty;
        #endif
        

        if (Translater.InitDone == false)
        {
            await ServiceLoader.Load<LanguageService>().Initialize();
        }

        ViewBag.Message = Translater.TranslateIfNeeded(message);
        return View("Login");
    }

    /// <summary>
    /// Performs a login
    /// </summary>
    /// <param name="model">the login model</param>
    /// <returns>the action result</returns>
    [HttpPost("authorize")]
    public async Task<IActionResult> PerformLogin([FromBody] AuthorizationModel model)
    {
        if (string.IsNullOrWhiteSpace(model?.Username))
            return BadRequest("Username is required");
        if (string.IsNullOrWhiteSpace(model?.Password))
            return BadRequest("Password is required");

        string ipAddress = Request.GetActualIP();
        var service = ServiceLoader.Load<UserService>();
        var result = await service.ValidateLogin(model.Username, model.Password, ipAddress);
        if (result.Failed(out string error))
        {
            var random = new Random(DateTime.Now.Millisecond);
            await Task.Delay(random.Next(1000, 3000));
            return BadRequest(error);
        }

        var settings = await ServiceLoader.Load<ISettingsService>().Get();

        var jwt = AuthenticationHelper.CreateJwtToken(result.Value, ipAddress, settings.TokenExpiryMinutes);
        return Ok(jwt);
    }

    /// <summary>
    /// Changes the users password
    /// </summary>
    /// <param name="model">the model</param>
    /// <returns>the response</returns>
    [HttpPost("authorize/change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
    {
        var security = ServiceLoader.Load<AppSettingsService>().Settings.Security;
        if (security != SecurityMode.Local)
            return BadRequest("Cannot change password");
        var user = await HttpContext.GetLoggedInUser();
        if(user == null)
            return BadRequest("Cannot change password");

        string newPassword = model.NewPassword.Trim();
        if (newPassword.Length < 5)
            return BadRequest("Dialogs.ChangePassword.MinimumLength");

        var service = ServiceLoader.Load<UserService>();
        var changed = await service.ChangePassword(user, model.OldPassword, newPassword);
        if (changed == false)
            return BadRequest("Dialogs.ChangePassword.InvalidPassword");
        await ServiceLoader.Load<AuditService>().AuditPasswordChange(user.Uid, user.Email, HttpContext.Request.GetActualIP());
        return Ok();
    }

    /// <summary>
    /// Resets the users password
    /// </summary>
    /// <param name="model">the reset model</param>
    [HttpPost("authorize/reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
    {
        if (string.IsNullOrWhiteSpace(model?.UsernameOrEmail))
            return BadRequest("Invalid username or email");
        var service = ServiceLoader.Load<UserService>();
        var user = await service.FindUser(model.UsernameOrEmail);
        if (user == null)
        {
            await Task.Delay(new Random(DateTime.UtcNow.Millisecond).Next(100, 1000));
            return Ok(); // dont say the user wasn't found, that will give away the user does or doesn't exist
        }

        var code = CodeHelper.CreateCode(user.Email, Claim, new TimeSpan(2, 0, 0));
        var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.PathBase}";
        string link = $"{baseUrl}/authorize/reset-password/{HttpUtility.UrlEncode(code)}";
        Console.WriteLine(link);
        
        _ = ServiceLoader.Load<IEmailService>().Send(user.Name, user.Email, "FileFlows Password Reset",
                $@"Dear {user.Name},

    We have received a request to reset your password for your account. To proceed with the password reset, please click on the following link:

    {link}

    Please note that this link is valid for 2 hours. After this period, the link will expire, and you will need to request a new password reset.

    If you did not initiate this password reset request, please disregard this email.

    If you need any assistance or have any questions, please contact our support team.

    Thank you.

    Best regards,
    The FileFlows Team
    ");
        
        await ServiceLoader.Load<AuditService>().AuditPasswordResetRequest(user.Uid, user.Name, HttpContext.Request.GetActualIP());
        
        return Ok();
    }
    
    
    /// <summary>
    /// Resets a users password
    /// </summary>
    /// <param name="code">the reset code</param>
    /// <returns>the IActionResult</returns>
    [HttpGet("authorize/reset-password/{code}")]
    public async Task<IActionResult> ResetPassword([FromRoute] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Redirect("/");

        var verificationResult = CodeHelper.VerifyCode(code, Claim);
        if(verificationResult.IsFailed)
            return Redirect("/error?msg=" + HttpUtility.UrlEncode("Password reset code is invalid or expired."));

        var email = verificationResult.Value;
        var service = ServiceLoader.Load<UserService>();
        var user = await service.FindUser(email);
        if(user == null)
            return Redirect("/error?msg=" + HttpUtility.UrlEncode("Password reset code is invalid or expired."));

        string newPassword = AuthenticationHelper.GenerateRandomPassword();
        
        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await service.Update(user, auditDetails: null); // null as we audit this manually
        
        await ServiceLoader.Load<AuditService>().AuditPasswordReset(user.Uid, user.Name, HttpContext.Request.GetActualIP());
        
        await ServiceLoader.Load<IEmailService>().Send(user.Email, user.Email, "FileFlows New Password",
            $@"Dear {user.Name},

We have generated a new password for your account.
Please use the following temporary password to login:

Username: {user.Name}
Password: {newPassword}

After logging in, we recommend that you change your password for security reasons.

Thank you!
Best regards,
The FileFlows Team");

        return Redirect("/login?pr=1");
    }



    /// <summary>
    /// Model for login
    /// </summary>
    public class AuthorizationModel
    {
        /// <summary>
        /// Gets or sets the username
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// Gets or sets the password
        /// </summary>
        public string Password { get; set; }
    }

    /// <summary>
    /// Change password model
    /// </summary>
    public class ChangePasswordModel
    {
        /// <summary>
        /// Gets or sets the old password
        /// </summary>
        public string OldPassword { get; set; }
        /// <summary>
        /// Gets or sets the new password
        /// </summary>
        public string NewPassword { get; set; }
    }

    /// <summary>
    /// Password reset model
    /// </summary>
    public class ResetPasswordModel
    {
        /// <summary>
        /// Gets or sets the username or email address of the user
        /// </summary>
        public string UsernameOrEmail { get; set; }
    }
}