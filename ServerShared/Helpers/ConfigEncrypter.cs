using System.Security.Cryptography;
using System.Text;

namespace FileFlows.ServerShared.Helpers;

/// <summary>
/// Provides methods for encrypting and decrypting configuration data securely using AES-256-CBC with HMAC-SHA256 verification.
/// </summary>
public static class ConfigEncrypter
{
    private const int SaltSize = 16;
    private const int IvSize = 16;
    private const int HmacSize = 32; // HMAC-SHA256 output size

    /// <summary>
    /// Encrypts the given JSON string and saves it to a file.
    /// </summary>
    /// <param name="json">The JSON content to encrypt.</param>
    /// <param name="outputFile">The output file path for the encrypted data.</param>
    public static void EncryptConfig(string json, string outputFile)
        => EncryptConfig(json, outputFile, MachineKeyProvider.GetMachineIdentifier());

    /// <summary>
    /// Encrypts the given JSON string and saves it to a file.
    /// </summary>
    /// <param name="json">The JSON content to encrypt.</param>
    /// <param name="password">The password used for encryption.</param>
    /// <param name="outputFile">The output file path for the encrypted data.</param>
    internal static void EncryptConfig(string json,  string outputFile, string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] iv = RandomNumberGenerator.GetBytes(IvSize);
        byte[] key = DeriveKey(password, salt);
        byte[] plaintext = Encoding.UTF8.GetBytes(json);

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        byte[] ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        byte[] hmac = ComputeHmac(salt, iv, ciphertext, key);

        using var fileStream = new FileStream(outputFile, FileMode.Create);
        fileStream.Write(salt);
        fileStream.Write(iv);
        fileStream.Write(hmac);
        fileStream.Write(ciphertext);
    }

    /// <summary>
    /// Decrypts an encrypted configuration file using the provided password.
    /// </summary>
    /// <param name="file">The encrypted file path.</param>
    /// <returns>The decrypted JSON string.</returns>
    /// <exception cref="CryptographicException">Thrown if decryption fails or integrity check fails.</exception>
    public static string DecryptConfig(string file)
        => DecryptConfig(file, MachineKeyProvider.GetMachineIdentifier());
    
    /// <summary>
    /// Decrypts an encrypted configuration file using the provided password.
    /// </summary>
    /// <param name="file">The encrypted file path.</param>
    /// <param name="password">The password used for decryption.</param>
    /// <returns>The decrypted JSON string.</returns>
    /// <exception cref="CryptographicException">Thrown if decryption fails or integrity check fails.</exception>
    internal static string DecryptConfig(string file, string password)
    {
        using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
        byte[] salt = new byte[SaltSize];
        byte[] iv = new byte[IvSize];
        byte[] hmac = new byte[HmacSize];

        fileStream.Read(salt, 0, SaltSize);
        fileStream.Read(iv, 0, IvSize);
        fileStream.Read(hmac, 0, HmacSize);
        
        byte[] ciphertext = new byte[fileStream.Length - SaltSize - IvSize - HmacSize];
        fileStream.Read(ciphertext, 0, ciphertext.Length);

        byte[] key = DeriveKey(password, salt);
        byte[] computedHmac = ComputeHmac(salt, iv, ciphertext, key);

        if (!CryptographicOperations.FixedTimeEquals(hmac, computedHmac))
            throw new CryptographicException("Integrity check failed: Data has been tampered with.");

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        byte[] plaintext = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        return Encoding.UTF8.GetString(plaintext);
    }

    /// <summary>
    /// Derives a cryptographic key from the given password and salt using SHA-256.
    /// </summary>
    /// <param name="password">The password used for key derivation.</param>
    /// <param name="salt">The salt used for key derivation.</param>
    /// <returns>A 32-byte encryption key.</returns>
    private static byte[] DeriveKey(string password, byte[] salt)
    {
        using var sha256 = SHA256.Create();
        byte[] passBytes = Encoding.UTF8.GetBytes(password);
        byte[] combined = new byte[passBytes.Length + salt.Length];
        Buffer.BlockCopy(passBytes, 0, combined, 0, passBytes.Length);
        Buffer.BlockCopy(salt, 0, combined, passBytes.Length, salt.Length);
        return sha256.ComputeHash(combined);
    }

    /// <summary>
    /// Computes the HMAC-SHA256 hash for integrity verification.
    /// </summary>
    /// <param name="salt">The salt used in encryption.</param>
    /// <param name="iv">The IV used in encryption.</param>
    /// <param name="ciphertext">The encrypted data.</param>
    /// <param name="key">The encryption key.</param>
    /// <returns>The computed HMAC hash.</returns>
    private static byte[] ComputeHmac(byte[] salt, byte[] iv, byte[] ciphertext, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        hmac.TransformBlock(salt, 0, salt.Length, null, 0);
        hmac.TransformBlock(iv, 0, iv.Length, null, 0);
        hmac.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        return hmac.Hash!;
    }
}
