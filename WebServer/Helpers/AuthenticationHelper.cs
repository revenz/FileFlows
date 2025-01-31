using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FileFlows.Services;
using FileFlows.Shared.Models;
using Microsoft.IdentityModel.Tokens;

namespace FileFlows.WebServer.Helpers;

/// <summary>
/// Authentication helper
/// </summary>
public class AuthenticationHelper
{
    /// <summary>
    /// Gets the user security mode in in use
    /// </summary>
    /// <returns>the user security mode</returns>
    public static SecurityMode GetSecurityMode()
    {
        if (LicenseService.IsLicensed(LicenseFlags.UserSecurity) == false)
            return SecurityMode.Off;
        
        var settings = ServiceLoader.Load<AppSettingsService>().Settings;
        if (settings.Security is SecurityMode.Local or SecurityMode.Off)
            return settings.Security;
        if (LicenseService.IsLicensed(LicenseFlags.SingleSignOn) == false)
            return SecurityMode.Off;
        return settings.Security;
    }
    
    /// <summary>
    /// Creates a JWT Token for an email address
    /// </summary>
    /// <param name="user">the user</param>
    /// <param name="ipAddress">the IP Address of the user</param>
    /// <param name="expiryMinutes">the number of minutes until his token expires</param>
    /// <returns>the JWT token</returns>
    public static string CreateJwtToken(User user, string ipAddress, int expiryMinutes)
    {
        if (expiryMinutes < 1)
            expiryMinutes = 24 * 60;
        string ip = ipAddress.Replace(":", "_");
        string code = FileFlows.Helpers.Decrypter.Encrypt(user.Uid + ":" + ip + ":" + user.Name);
        var claims = new List<Claim>
        {
            new (ClaimTypes.Name, user.Name),
            new (ClaimTypes.Email, user.Email),
            new Claim("code", code)
        };

        var service = ServiceLoader.Load<UserService>();
        _ = service.RecordLogin(user, ipAddress);
    
        var claimsIdentity = new ClaimsIdentity(claims);

        var key = ServiceLoader.Load<AppSettingsService>().Settings.EncryptionKey;
    
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claimsIdentity,
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = "https://fileflows.com",
            Audience = "https://fileflows.com",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a random password with specified length and character requirements.
    /// </summary>
    /// <param name="length">Length of the password to generate.</param>
    /// <returns>A randomly generated password.</returns>
    public static string GenerateRandomPassword(int length = 20)
    {
        Random random = new Random(DateTime.UtcNow.Millisecond);
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        StringBuilder password = new StringBuilder();
        int numSpecialChars = random.Next(2, 4); // Randomly select 2 or 3 special characters
        int numLetters = length - numSpecialChars;

        // Generate random letters
        for (int i = 0; i < numLetters; i++)
        {
            password.Append(validChars[random.Next(validChars.Length)]);
        }

        // Generate random special characters
        string specialChars = "!@#$%^&*()-_=+";
        for (int i = 0; i < numSpecialChars; i++)
        {
            password.Append(specialChars[random.Next(specialChars.Length)]);
        }

        // Shuffle the password characters
        for (int i = 0; i < length; i++)
        {
            int randomIndex = random.Next(length);
            (password[i], password[randomIndex]) = (password[randomIndex], password[i]);
        }

        return password.ToString();
    }
}