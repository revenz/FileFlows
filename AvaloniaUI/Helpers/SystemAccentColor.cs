using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Media;

namespace FileFlows.AvaloniaUi.Helpers;

/// <summary>
/// Provides methods to retrieve the system's accent color.
/// </summary>
public static class SystemAccentColor
{
    /// <summary>
    /// Gets the operating system's accent color.
    /// </summary>
    /// <returns>The detected accent color, or a default fallback color.</returns>
    public static Color GetOsAccentColor()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return GetWindowsAccentColor();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return GetMacOsAccentColor();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return GetLinuxAccentColor();

        return Colors.DodgerBlue; // Default fallback
    }

    /// <summary>
    /// Retrieves the accent color for Windows.
    /// </summary>
    /// <returns>The Windows accent color, or a default color if unavailable.</returns>
    private static Color GetWindowsAccentColor()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
                var value = key?.GetValue("AccentColor") as int?;
                if (value.HasValue)
                {
                    uint colorValue = (uint)value.Value;
                    return Color.FromArgb(
                        255,
                        (byte)((colorValue >> 16) & 0xFF), // R
                        (byte)((colorValue >> 8) & 0xFF), // G
                        (byte)(colorValue & 0xFF) // B
                    );
                }
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        return Colors.DodgerBlue;
    }

    /// <summary>
    /// Retrieves the accent color for macOS.
    /// </summary>
    /// <returns>A fallback accent color, as macOS does not expose this easily.</returns>
    private static Color GetMacOsAccentColor()
    {
        return Colors.CornflowerBlue; // macOS doesn't expose accent color easily
    }

    /// <summary>
    /// Retrieves the accent color for Linux.
    /// </summary>
    /// <returns>The detected accent color for Linux, or a default fallback color.</returns>
    private static Color GetLinuxAccentColor()
    {
        if (SystemHelper.IsKDE)
            return GetKdeAccentColor();
        if (SystemHelper.IsGnome)
            return GetGnomeAccentColor();

        return Colors.MediumSeaGreen; // Default for unknown Linux desktops
    }
    
    /// <summary>
    /// Retrieves the accent color for KDE environments.
    /// </summary>
    /// <returns>The KDE accent color, or a fallback if unavailable.</returns>
    private static Color GetKdeAccentColor()
    {
        try
        {
            string kdeConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config/kdeglobals");
            if (File.Exists(kdeConfigPath))
            {
                var lines = File.ReadAllLines(kdeConfigPath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("AccentColor="))
                    {
                        string colorHex = line.Split('=')[1].Trim();
                        return ParseHexColor(colorHex);
                    }
                }
            }
        }
        catch (Exception) { }

        return Colors.MediumPurple; // Fallback for KDE
    }


    /// <summary>
    /// Parses a hexadecimal color string and returns a <see cref="Color"/>.
    /// </summary>
    /// <param name="hex">The hexadecimal color string.</param>
    /// <returns>The parsed <see cref="Color"/>.</returns>
    private static Color ParseHexColor(string hex)
    {
        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        if (hex.Length == 6)
        {
            return Color.FromRgb(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16)
            );
        }

        return Colors.DodgerBlue; // Fallback
    }
    

    /// <summary>
    /// Retrieves the accent color for GNOME environments.
    /// </summary>
    /// <returns>The GNOME accent color, or a fallback if unavailable.</returns>
    private static Color GetGnomeAccentColor()
    {
        try
        {
            string output = RunShellCommand("gsettings get org.gnome.desktop.interface accent-color").Trim();
        
            if (!string.IsNullOrWhiteSpace(output) && output.StartsWith("'") && output.EndsWith("'"))
                output = output.Trim('\''); // Remove single quotes

            return output.ToLower() switch
            {
                "default" => Colors.DodgerBlue,
                "red" => Colors.Red,
                "green" => Colors.Green,
                "blue" => Colors.Blue,
                "purple" => Colors.Purple,
                "orange" => Colors.Orange,
                "brown" => Colors.SaddleBrown,
                "lightgreen" => Colors.LightGreen,
                "pink" => Colors.HotPink,
                "teal" => Colors.Teal,
                "yellow" => Colors.Yellow,
                "lightblue" => Colors.LightBlue,
                "lightpurple" => Color.FromRgb(187, 134, 252), // Closer match
                _ => Colors.CadetBlue // Fallback
            };
        }
        catch (Exception)
        {
            return Colors.CadetBlue;
        }
    }
    
    /// <summary>
    /// Runs a shell command and returns the output.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>The standard output from the executed command.</returns>
    private static string RunShellCommand(string command)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            using var reader = process?.StandardOutput;
            return reader?.ReadToEnd().Trim() ?? "";
        }
        catch (Exception) { return ""; }
    }
}