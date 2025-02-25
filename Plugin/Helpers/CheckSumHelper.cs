using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace FileFlows.Plugin.Helpers;

/// <summary>
/// Helper class for computing checksums using various hashing algorithms.
/// </summary>
public class CheckSumHelper
{
    /// <summary>
    /// Computes the MD5 checksum for the given string.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The MD5 checksum as a hexadecimal string.</returns>
    public string MD5(string input) => ComputeHash(input, System.Security.Cryptography.MD5.Create());

    /// <summary>
    /// Computes the MD5 checksum for the given binary data.
    /// </summary>
    /// <param name="data">The binary data.</param>
    /// <returns>The MD5 checksum as a hexadecimal string.</returns>
    public string MD5(byte[] data) => ComputeHash(data, System.Security.Cryptography.MD5.Create());

    /// <summary>
    /// Computes the MD5 checksum for a given file.
    /// </summary>
    /// <param name="filename">The file path.</param>
    /// <returns>The MD5 checksum as a hexadecimal string.</returns>
    public string MD5File(string filename) => ComputeFileHash(filename, System.Security.Cryptography.MD5.Create());

    /// <summary>
    /// Computes the SHA-1 checksum for the given string.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The SHA-1 checksum as a hexadecimal string.</returns>
    public string SHA1(string input) => ComputeHash(input, System.Security.Cryptography.SHA1.Create());

    /// <summary>
    /// Computes the SHA-1 checksum for the given binary data.
    /// </summary>
    /// <param name="data">The binary data.</param>
    /// <returns>The SHA-1 checksum as a hexadecimal string.</returns>
    public string SHA1(byte[] data) => ComputeHash(data, System.Security.Cryptography.SHA1.Create());

    /// <summary>
    /// Computes the SHA-1 checksum for a given file.
    /// </summary>
    /// <param name="filename">The file path.</param>
    /// <returns>The SHA-1 checksum as a hexadecimal string.</returns>
    public string SHA1File(string filename) => ComputeFileHash(filename, System.Security.Cryptography.SHA1.Create());

    /// <summary>
    /// Computes the SHA-256 checksum for the given string.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The SHA-256 checksum as a hexadecimal string.</returns>
    public string SHA256(string input) => ComputeHash(input, System.Security.Cryptography.SHA256.Create());

    /// <summary>
    /// Computes the SHA-256 checksum for the given binary data.
    /// </summary>
    /// <param name="data">The binary data.</param>
    /// <returns>The SHA-256 checksum as a hexadecimal string.</returns>
    public string SHA256(byte[] data) => ComputeHash(data, System.Security.Cryptography.SHA256.Create());

    /// <summary>
    /// Computes the SHA-256 checksum for a given file.
    /// </summary>
    /// <param name="filename">The file path.</param>
    /// <returns>The SHA-256 checksum as a hexadecimal string.</returns>
    public string SHA256File(string filename) => ComputeFileHash(filename, System.Security.Cryptography.SHA256.Create());

    /// <summary>
    /// Computes the SHA-512 checksum for the given string.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The SHA-512 checksum as a hexadecimal string.</returns>
    public string SHA512(string input) => ComputeHash(input, System.Security.Cryptography.SHA512.Create());

    /// <summary>
    /// Computes the SHA-512 checksum for the given binary data.
    /// </summary>
    /// <param name="data">The binary data.</param>
    /// <returns>The SHA-512 checksum as a hexadecimal string.</returns>
    public string SHA512(byte[] data) => ComputeHash(data, System.Security.Cryptography.SHA512.Create());

    /// <summary>
    /// Computes the SHA-512 checksum for a given file.
    /// </summary>
    /// <param name="filename">The file path.</param>
    /// <returns>The SHA-512 checksum as a hexadecimal string.</returns>
    public string SHA512File(string filename) => ComputeFileHash(filename, System.Security.Cryptography.SHA512.Create());

    /// <summary>
    /// Computes a hash for the given string using the specified hash algorithm.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="algorithm">The hash algorithm.</param>
    /// <returns>The computed hash as a hexadecimal string.</returns>
    private string ComputeHash(string input, HashAlgorithm algorithm)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        return ComputeHash(inputBytes, algorithm);
    }

    /// <summary>
    /// Computes a hash for the given binary data using the specified hash algorithm.
    /// </summary>
    /// <param name="data">The binary data.</param>
    /// <param name="algorithm">The hash algorithm.</param>
    /// <returns>The computed hash as a hexadecimal string.</returns>
    private string ComputeHash(byte[] data, HashAlgorithm algorithm)
    {
        using (algorithm)
        {
            byte[] hashBytes = algorithm.ComputeHash(data);
            return BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Computes a hash for the given file using the specified hash algorithm without loading the entire file into memory.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="algorithm">The hash algorithm.</param>
    /// <returns>The computed hash as a hexadecimal string.</returns>
    private string ComputeFileHash(string filePath, HashAlgorithm algorithm)
    {
        using (algorithm)
        using (var stream = File.OpenRead(filePath)) // Minimum required to read the file
        {
            byte[] hashBytes = algorithm.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
