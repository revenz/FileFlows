using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace FileFlows.ServerShared.Helpers;

internal static class MachineKeyProvider
{
    /// <summary>
    /// Cached key to avoid multiple lookups
    /// </summary>
    private static string _cachedKey;

    /// <summary>
    /// Static constructor
    /// </summary>
    static MachineKeyProvider()
    {
        _cachedKey = GetPlatformMachineIdentifier();
    }

    /// <summary>
    /// Retrieves a stable machine identifier based on OS-specific methods.
    /// </summary>
    /// <returns>A machine-specific string identifier.</returns>
    public static string GetMachineIdentifier()
        => _cachedKey;

    /// <summary>
    /// Retrieves a stable machine identifier based on OS-specific methods.
    /// </summary>
    /// <returns>A machine-specific string identifier.</returns>
    private static string GetPlatformMachineIdentifier()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetWindowsMachineGuid();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return GetLinuxMachineId();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetMacMachineId();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[MachineKeyProvider] Error retrieving machine identifier: {ex.Message}");
        }

        // Fallback: Generate a machine identifier based on hostname
        return GetFallbackMachineId();
    }

    /// <summary>
    /// Retrieves the machine GUID on Windows from the registry.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static string GetWindowsMachineGuid()
    {
        if (OperatingSystem.IsWindows() == false)
            throw new InvalidOperationException("Not windows");
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            var guid = key?.GetValue("MachineGuid")?.ToString();
            if (string.IsNullOrEmpty(guid))
                throw new Exception("MachineGuid not found in registry.");
            return guid;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to retrieve Windows MachineGuid.", ex);
        }
    }

    /// <summary>
    /// Retrieves the machine ID on Linux from /etc/machine-id or a fallback.
    /// </summary>
    private static string GetLinuxMachineId()
    {
        try
        {
            string path = "/etc/machine-id";
            if (File.Exists(path))
            {
                string id = File.ReadAllText(path).Trim();
                if (!string.IsNullOrWhiteSpace(id))
                    return id;
            }

            // Fallback: Try D-Bus machine ID
            path = "/var/lib/dbus/machine-id";
            if (File.Exists(path))
            {
                string id = File.ReadAllText(path).Trim();
                if (!string.IsNullOrWhiteSpace(id))
                    return id;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[MachineKeyProvider] Error retrieving Linux machine ID: {ex.Message}");
        }

        throw new InvalidOperationException("Linux machine ID could not be retrieved.");
    }

    /// <summary>
    /// Retrieves the machine identifier on macOS using system commands.
    /// </summary>
    private static string GetMacMachineId()
    {
        try
        {
            string result = RunCommand("ioreg -rd1 -c IOPlatformExpertDevice | grep IOPlatformUUID | cut -d '\"' -f 4").Trim();
            if (!string.IsNullOrWhiteSpace(result))
                return result;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[MachineKeyProvider] Error retrieving macOS machine ID: {ex.Message}");
        }

        throw new InvalidOperationException("macOS machine ID could not be retrieved.");
    }

    /// <summary>
    /// Runs a shell command and returns its output.
    /// </summary>
    private static string RunCommand(string command)
    {
        try
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            string error = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception($"Command failed: {error}");

            return output;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to execute command: {command}", ex);
        }
    }

    /// <summary>
    /// Fallback method: Uses a hash of the hostname as a machine identifier.
    /// </summary>
    private static string GetFallbackMachineId()
    {
        try
        {
            string hostName = Environment.MachineName;
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(hostName));
            return Convert.ToHexString(hash);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to generate fallback machine ID.", ex);
        }
    }
}
