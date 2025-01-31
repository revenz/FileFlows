using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FileFlows.Plugin;
using FileFlows.Services;
using Microsoft.IdentityModel.Tokens;

namespace FileFlows.WebServer.Helpers;

/// <summary>
/// Helper to reset a users password 
/// </summary>
class CodeHelper
{
    private const string Issuer = "FileFlows";
    private const string Audience = "https://fileflows.com";

    /// <summary>
    /// Generates a code for a user
    /// </summary>
    /// <param name="value">the value to store in the JWT</param>
    /// <param name="claim">the to store</param>
    /// <param name="expiry">hwo long this code will be valid for</param>
    /// <returns>the code</returns>
    public static string CreateCode(string value, string claim, TimeSpan expiry)
    {
        // Create claims for the token
        var claims = new[]
        {
            new Claim(claim, value)
        };

        string code = ServiceLoader.Load<AppSettingsService>().Settings.EncryptionKey;

        // Create the token
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(expiry),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(code)), 
                SecurityAlgorithms.HmacSha256)
        );

        // Serialize the token to a string
        return new JwtSecurityTokenHandler().WriteToken(token);    
    }

    /// <summary>
    /// Verifies the users code
    /// </summary>
    /// <param name="code">the code</param>
    /// <param name="claim">the claim to get</param>
    /// <returns>true if its valid, otherwise false</returns>
    public static Result<string> VerifyCode(string code, string claim)
    {
        try
        {
            string encryptionKey = ServiceLoader.Load<AppSettingsService>().Settings.EncryptionKey;
            
            // Validate and decode the token
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(encryptionKey)),
                ValidIssuer = Issuer,
                ValidAudience = Audience
            };

            ClaimsPrincipal claimsPrincipal = tokenHandler.ValidateToken(code, tokenValidationParameters, 
                out SecurityToken validatedToken);
            var emailAddressClaim = claimsPrincipal.FindFirst(claim);

            if (emailAddressClaim == null)
                return Result<string>.Fail("Code is invalid");

            // Extract the email address from the claim
            return emailAddressClaim.Value;
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(ex.Message);
        }
    }
    
    /// <summary>
    /// Performs a SHA-512 hash on a string.
    /// </summary>
    /// <param name="input">The input string to hash.</param>
    /// <returns>The hashed string.</returns>
    public static string Sha512String(string input)
    {
        using (SHA512 sha512 = SHA512.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = sha512.ComputeHash(inputBytes);
            string hashed = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            return hashed;
        }
    }
}
