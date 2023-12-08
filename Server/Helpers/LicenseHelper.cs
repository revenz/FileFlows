using System.Security.Cryptography;
using System.Text;
using FileFlows.Shared.Helpers;

namespace FileFlows.Server.Helpers;

/// <summary>
/// Helper for licensing
/// </summary>
class LicenseHelper
{
    const string LicenseResponse_DecryptionKey = "MIIEowIBAAKCAQEAqdurpq85Xtg0haj0LHl//hKBlAFyX4Vsuo2xhIScoYqHUEGoljEZUZdiOe764kiNKOQEqpzbXijoyxGLy4mDtDkEa90L21gbn31mekiIuODdEW9IKmPftrB182hTWYUUg1VlTunkVLVln/ywdf1BfcAHeQn9EcmxwDbqovmMTspKqxrVYgbtEpZZybqbwnyo9m+AKeU+xag3zoAnw2q4OejMLm00BF/uhYDK6uSMQC1Qy15j06KIjY65YT5meDqfkfWsxOEpZ2uCiollFOYsvZPyrNGd6fvKGDD2fKAlvAwSn0PAsMAOUEm+QlZtne4pIADiVnMM9RX7f4OtvVtaXQIDAQABAoIBAAguqc0kwbm85oLNyb1euPivQYi0rSLG1Z8C9lsw3C638p6+GvXpNZQFm9i4l2NRJWOj4EmrtrGJfPVTSg2q+So0WO8tPcX6L5J2Qvp/Bf8J7fxKBQrttrghNf1cuC8mxv8wnOm5QKOH/XZAgOueIIqDNpjxDzzNH3/n5VOme8jLx8PGMCwJWMFK0XdNObD8Dvzv3J9y3X1EVrB/Jm9pcc/2OMieL1kKrJmX3YvA5aXHB6cjbFDL5xwRb9wEOJlx/KlRQQVg7dLVhyMNRXc+gxLPvOR3cbeZ3nb8nGTJeJDSxq1p8+z61wQF5L5Kxche/hZdu1D2rGFOQHUjbrHEJE0CgYEAyFcoChhv9q9RgVwbk26ErIOejcVILIFlMcxbpRoJ/PD4ltXoz7u3UhBT+zsSPRxvT5gPZ6qqEIG5uWlnd1YOdS0KrtSM7K/nBI9IQoN65gTBTMzXSDkuZuZGvs726b4rhvuhDJa6uRyDkKpcsNQkCQ+KUIfnWWYkKq5oJba7S/cCgYEA2QyDI1UkuVtGkWJLL7h1g95abfLOIreV0czMc9uVBCbL+2EyQC44vIwqsRDZkx5/XlvY4bT+o6zAKmwdmCa/QOmWY1SkGBVkDwD6i4mOHnmrV+5icF2OiaSmjtFFOOLteq0SauIWhjEGQgNgoldj6kXVtqVvIUY8rj2yDBrUb0sCgYEAiCYDBelZnbHDmD/6VZVUANFp3TrnM6e0F8Wjum4Zv5YbupYgo5wUl2aVTDT2ziUW2GakgXUQIiunBgRF1mnbZXJ4whucsfVQ8F5XYyxrRwqQOxsyatjBWhjAl0ebsXoVpqQ27JE60DY6iwPb/igNXUL8YoIZjT3G8mKYUJkAbD0CgYBlE+aeNbB8gX1DhzrsZkKTvqDuQvysPkKPCYjNC51B6a9kycbVDLFvXPckrmwkjzdRggRmWBudrX1wRBkkGidG24ElkO06KfwG4LXM9aoxlwesU1+UZH1UrFDEgcBy1Xsyfhbtn4xNwdbgNyJxd7EYEJ2OCUzPeh4YJrMb4AK+MQKBgBXolFhtJWGrKHSfAgXdLMltDErrAO0t0vvQzdZEQExXRhnx3KPfpHMwg5x13bN95aCvodR+136csEOk6UqSuH5xAtZWWkZQE5umBNm1DJs68cERIC6XDishlXkXgAL9qkurmSJ7RK5dcCfZI/uM6BBjs43IqfVUUKmt8Gqbedry";
    const string LicenseRequest_EncryptionKey = "MIIBCgKCAQEAtMPKGqGr2pYyaMvoxE8d6rlL//Rl7be9AqA4inKvAc0MWmGy6MaiWvX2YHJfaddNSo3CXIgt48KQAUte/+ZM5Nja/cYECPDIS51ragsTfSK/jW5WVsOw8GzZlCV0rcQHQJ+MtNb6lBZD89ffOkQZHAQuC8lh4ptHmnQ3nupnUhlQGOAfnHQSqiDV/BUKcJINQAYMmrVHQJwAm1iXz6xq+dOhzaf+aJ28oRLanEsPcfwZpfkhlxCavMIkQNfIiVJBX89aw4U9yAgMbNhwFr9Zy6lOLyjjHNitOGrgEl1CEgsE04DUQWx2OHmN44rrxv1CQn/vam0G8PHzognbqtw0EwIDAQAB";

    private static DateTime LastUpdate = DateTime.MinValue;
    private static string LastLicenseEmail, LastLicenseKey;
    private static License LastLicense;

    /// <summary>
    /// Checks if the user is licensed for a feature
    /// </summary>
    /// <param name="feature">the feature to check</param>
    /// <returns>true if licensed, otherwise false</returns>
    internal static bool IsLicensed(LicenseFlags feature)
    {
        var license = GetLicense();
        if (license?.Status != LicenseStatus.Valid)
            return false;
        if (license.ExpirationDateUtc < DateTime.UtcNow)
            return false;
        return (license.Flags & feature) == feature;
    }

    
    /// <summary>
    /// Checks if the user is licensed 
    /// </summary>
    /// <returns>true if licensed, otherwise false</returns>
    internal static bool IsLicensed()
    {
        var license = GetLicense();
        if (license?.Status != LicenseStatus.Valid)
            return false;
        if (license.ExpirationDateUtc < DateTime.UtcNow)
            return false;
        return true;
    }

    
    /// <summary>
    /// Gets the amount of nodes this user is licensed for
    /// </summary>
    /// <returns>the amount of nodes this user is licensed for</returns>
    internal static int GetLicensedProcessingNodes()
    {
        var license = GetLicense();
        if (license?.Status != LicenseStatus.Valid)
            return 2;
        if (license.ExpirationDateUtc < DateTime.UtcNow)
            return 2;
        if (license.ProcessingNodes < 2)
            return 2;
        return license.ProcessingNodes;
    }


    /// <summary>
    /// Gets the license
    /// </summary>
    /// <returns>the license</returns>
    internal static License GetLicense() => LastLicense ?? FromCode(AppSettings.Instance.LicenseCode);
    
    internal static License? FromCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;
        try
        {
            string decrypted = Decrypt(LicenseResponse_DecryptionKey, code);
            return JsonSerializer.Deserialize<License>(decrypted);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Update the license code 
    /// </summary>
    internal static async Task Update()
    {
        var email = AppSettings.Instance.LicenseEmail;
        var key = AppSettings.Instance.LicenseKey;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(key))
        {
            AppSettings.Instance.LicenseCode = string.Empty;
            AppSettings.Instance.Save();
            return;
        }

        if (LastLicenseEmail == email && LastLicenseKey == key && LastUpdate > DateTime.Now.AddMinutes(-5))
            return; // last update wasn't long ago, can skip it
        try
        {
            string json = JsonSerializer.Serialize(new LicenseValidationModel
            {
                Key = key,
                EmailAddress = email
            });

            string requestCode = Encrypt(LicenseRequest_EncryptionKey, json);
            
            string licenseUrl = Globals.FileFlowsDotComUrl + "/licensing/validate";
            var result = await HttpHelper.Post(licenseUrl, new { Code = requestCode });
            if (result.Success == false)
                return;

            string licenseCode = result.Body;
            var license = FromCode(licenseCode);
            if (license == null)
                return;
            
            // could reach the server, license request was good, record it.
            LastLicense = license;
            LastLicenseEmail = email;
            LastLicenseKey = key;
            LastUpdate = DateTime.Now;

            // code is good, save it
            AppSettings.Instance.LicenseCode = licenseCode;
            AppSettings.Instance.Save();
        }
#if(DEBUG)
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed validating license: " + ex.Message + "\n" + ex.Message);
        }
#else
        catch (Exception) { }
#endif
    }
    
    class LicenseValidationModel
    {
        public string EmailAddress { get; set; }
        public string Key { get; set; }
    }

    #region encryption/decryption
    /// <summary>
    /// Encrypts a string
    /// </summary>
    /// <param name="key">the encryption key as a base64 string</param>
    /// <param name="data">the string to encrypt</param>
    /// <returns>the string encrypted as a base64 string</returns>
    static string Encrypt(string key, string? data)
    {
        if (string.IsNullOrEmpty(data))
            return string.Empty;
        
        RSA rsa = RSA.Create();

        var cipher = new RSACryptoServiceProvider();
        byte[] publicKey = Convert.FromBase64String(key);
        cipher.ImportRSAPublicKey(publicKey, out int bytesRead);
        byte[] bData = Encoding.UTF8.GetBytes(data);
        byte[] cipherText = cipher.Encrypt(bData, false);
        return Convert.ToBase64String(cipherText);
    }
    
    /// <summary>
    /// Encrypts a string
    /// </summary>
    /// <param name="key">the encryption key as a base64 string</param>
    /// <param name="data">the string to decrypt</param>
    /// <returns>the string decrypted</returns>
    static string Decrypt(string key, string? data)
    {
        if (string.IsNullOrEmpty(data))
            return string.Empty;
        try
        {
            var cipher = new RSACryptoServiceProvider();
            byte[] privateKey = Convert.FromBase64String(key);
            cipher.ImportRSAPrivateKey(privateKey, out int bytesRead);

            byte[] ciphterText = Convert.FromBase64String(data);
            byte[] plainText = cipher.Decrypt(ciphterText, false);
            string result = Encoding.UTF8.GetString(plainText);

            return result;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }    
    #endregion
}