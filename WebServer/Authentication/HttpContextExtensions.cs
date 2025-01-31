using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FileFlows.Plugin;
using FileFlows.Server.Helpers;
using FileFlows.Services;
using FileFlows.Shared.Models;
using Microsoft.IdentityModel.Tokens;

namespace FileFlows.WebServer.Authentication;

/// <summary>
/// Extension methods on the HttpContext
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the logged in user
    /// </summary>
    /// <param name="context">the HttpContext</param>
    /// <param name="requireActivated">[Optional] when true, if the user is not activated, will return null</param>
    /// <returns>the logged in user</returns>
    public static async Task<User?> GetLoggedInUser(this HttpContext context, bool requireActivated = true)
    {
        try
        {
            if (context == null)
                return null;

            var headerResult = ValidateAuthHeader(context);
            if (headerResult.IsFailed)
                return null;

            var validatedToken = headerResult.Value;
            var claims = ((JwtSecurityToken)validatedToken).Claims;
            var codeClaim = claims.FirstOrDefault(c => c.Type == "code")?.Value;
            if (string.IsNullOrWhiteSpace(codeClaim))
                return null;
            var code = DataLayer.Helpers.Decrypter.Decrypt(codeClaim);
            if (string.IsNullOrWhiteSpace(code) || code.IndexOf(":", StringComparison.Ordinal) < 1)
                return null;

            var parts = code.Split(':');
            if (parts.Length != 3)
                return null;

            if (Guid.TryParse(parts[0], out Guid uid) == false)
                return null;

            string expectedIp = parts[1].Replace("_", ":");
            string ip = context?.Request?.GetActualIP() ?? string.Empty;
            #if(DEBUG)
            if (ip == "127.0.0.1")
                ip = "::1";
            if (expectedIp == "127.0.0.1")
                expectedIp = "::1";
            #endif
            if (string.IsNullOrEmpty(ip) || ip != expectedIp)
                return null;

            var user = await new UserService().GetByUid(uid);
            if (user == null)
                return null;

            if (parts[2] != user.Name)
                return null;

            return user;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Validates the auth header
    /// </summary>
    /// <param name="context">the HTTP context</param>
    /// <returns>the validation result</returns>
    private static Result<SecurityToken> ValidateAuthHeader(HttpContext context)
    {
        var jwt = context.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(jwt) || jwt.StartsWith("Bearer ") == false)
            return Result<SecurityToken>.Fail("No Authorization header");
        jwt = jwt[7..]; // remove "Bearer "

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = ServiceLoader.Load<AppSettingsService>().Settings.EncryptionKey;
        var jwtKey = Encoding.ASCII.GetBytes(key);
        try
        {
            tokenHandler.ValidateToken(jwt, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "https://fileflows.com",
                ValidAudience = "https://fileflows.com",
                IssuerSigningKey = new SymmetricSecurityKey(jwtKey)
            }, out var validatedToken);

            if (validatedToken.ValidTo < DateTime.UtcNow)
                throw new Exception("Token has expired");

            // Token is valid, proceed with the request.
            return validatedToken;
        }
        catch (Exception ex)
        {
            return Result<SecurityToken>.Fail(ex.Message);
        }
    }
}