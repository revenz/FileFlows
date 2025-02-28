using System.Diagnostics;
using System.Globalization;

namespace FileFlows.AvaloniaUi.Helpers;

/// <summary>
/// Provides methods to retrieve the system text scaling factor for different operating systems.
/// </summary>
public static class SystemTextScale
{
    /// <summary>
    /// Gets the system text scaling factor based on the operating system.
    /// </summary>
    /// <returns>The text scaling factor as a double.</returns>
    public static double GetSystemTextScalingFactor()
    {
        if (OperatingSystem.IsWindows())
            return GetWindowsScaling();
        if (OperatingSystem.IsMacOS())
            return GetMacOSScaling();
        if (OperatingSystem.IsLinux())
            return GetLinuxScaling(); // GNOME / KDE

        return 1.0; // Default scale
    }

    /// <summary>
    /// Retrieves the text scaling factor for Windows.
    /// </summary>
    /// <returns>The text scaling factor, or 1.0 if retrieval fails.</returns>
    private static double GetWindowsScaling()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
                if (key != null && key.GetValue("LogPixels") is int dpi)
                    return dpi / 96.0; // Windows default DPI is 96, so we normalize it

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read Windows text scale: {ex.Message}");
            }
        }

        return 1.0; // Default scale if registry access fails
    }

    /// <summary>
    /// Retrieves the text scaling factor for macOS.
    /// </summary>
    /// <returns>The text scaling factor, or 1.0 if retrieval fails.</returns>
    private static double GetMacOSScaling()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "defaults",
                    Arguments = "read -g AppleFontSmoothing",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (int.TryParse(output, out int smoothing) && smoothing > 1)
                return 1.25; // macOS applies 1.25x scaling when font smoothing is enabled
        }
        catch
        {
            // ignored
        }

        return 1.0;
    }


    /// <summary>
    /// Retrieves the text scaling factor for Linux.
    /// </summary>
    /// <returns>The text scaling factor, or 1.0 if retrieval fails.</returns>
    private static double GetLinuxScaling()
    {
        if(SystemHelper.IsGnome)
            return GetGnomeScaling() ?? 1.0;
        if(SystemHelper.IsKDE)
            return GetKdeScaling() ?? 1.0;
        return 1.0;
    }

    /// <summary>
    /// Retrieves the text scaling factor for GNOME desktop environments.
    /// </summary>
    /// <returns>The text scaling factor, or null if retrieval fails.</returns>
    private static double? GetGnomeScaling()
    {
        return TryExecuteCommand("gsettings get org.gnome.desktop.interface text-scaling-factor");
    }
    
        
    /// <summary>
    /// Retrieves the text scaling factor for KDE desktop environments.
    /// </summary>
    /// <returns>The text scaling factor, or null if retrieval fails.</returns>
    private static double? GetKdeScaling()
    {
        double? dpiScale = TryExecuteCommand("kreadconfig5 --group General --key Xft.dpi");
        return dpiScale switch
        {
            > 0 => dpiScale.Value / 96.0,
            _ => null
        };
    }

    /// <summary>
    /// Executes a shell command and attempts to parse the output as a double.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>The parsed double value, or null if parsing fails.</returns>
    private static double? TryExecuteCommand(string command)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (double.TryParse(output, NumberStyles.Any, CultureInfo.InvariantCulture, out double scale))
                return scale;
        }
        catch
        {
            // ignored
        }

        return null;
    }
}