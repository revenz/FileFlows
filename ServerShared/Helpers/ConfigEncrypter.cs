using System.Security.Cryptography;

namespace FileFlows.ServerShared.Helpers;

class ConfigEncrypter
{
    private const int SaltSize = 8;

    internal static void Save(string json, string password, string outputFile)
    {
#pragma warning disable SYSLIB0041
        var keyGenerator = new Rfc2898DeriveBytes(password, SaltSize);
#pragma warning restore SYSLIB0041
        var aes = Aes.Create();

        // BlockSize, KeySize in bit --> divide by 8
        aes.Padding = PaddingMode.PKCS7;
        aes.BlockSize = 128;
        aes.Key = keyGenerator.GetBytes(aes.BlockSize / 8);
        aes.IV = keyGenerator.GetBytes(aes.BlockSize / 8);

        using var fileStream = new FileInfo(outputFile).Create();
        // write random salt
        fileStream.Write(keyGenerator.Salt, 0, SaltSize);

        using var cryptoStream = new CryptoStream(fileStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

        // write data
        StreamWriter writer = new StreamWriter(cryptoStream);
        writer.Write(json);
        writer.Flush();
        cryptoStream.Flush();
    }

}