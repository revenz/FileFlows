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
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
                var value = key?.GetValue("AccentColor") as int?;
                if (value.HasValue)
                {
                    uint colorValue = (uint)value.Value;
    
                    // Windows stores color as ABGR, need to convert to ARGB
                    return Color.FromArgb(
                        255, // Force full opacity
                        (byte)(colorValue & 0xFF),       // R (Swap with B)
                        (byte)((colorValue >> 8) & 0xFF),  // G
                        (byte)((colorValue >> 16) & 0xFF) // B
                    );
                }
            }
            catch (Exception)
            {
                // Ignore errors and return fallback color
            }
        }
    
        return Colors.DodgerBlue; // Fallback if not Windows or registry fails
    }
    
    /// <summary>
    /// Retrieves the accent color for macOS by executing a shell command.
    /// </summary>
    private static Color GetMacOsAccentColor()
    {
        try
        {
            string accentColorIndex = RunShellCommand("defaults read -g AppleAccentColor").Trim();
            if (string.IsNullOrEmpty(accentColorIndex))
                return Colors.CornflowerBlue; // Fallback if not found
            
            return accentColorIndex switch
            {
                "0" => Colors.Red,
                "1" => Colors.Orange,
                "2" => Colors.Goldenrod,
                "3" => Colors.Green,
                "4" => Colors.Blue,
                "5" => Colors.Purple,
                "6" => Colors.HotPink,
                "-1" => Color.FromRgb(85,85,85),
                _ => Colors.CornflowerBlue, // Default fallback color
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed getting accent color: " + ex.Message);
            return Colors.CornflowerBlue; // Return fallback color if error occurs
        }
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