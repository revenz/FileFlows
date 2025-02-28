namespace FileFlows.AvaloniaUi.Helpers;

/// <summary>
/// Provides helper methods to detect system environment details like desktop environment.
/// </summary>
public class SystemHelper
{
    /// <summary>
    /// Static constructor to initialize system desktop environment flags.
    /// This constructor checks the environment variables to determine if the system is running KDE or GNOME.
    /// It only performs these checks if the operating system is Linux.
    /// </summary>
    static SystemHelper()
    {
        // Only check the desktop environment if the system is Linux
        if (OperatingSystem.IsLinux())
        {
            var desktopEnv = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? string.Empty;
            IsKDE = desktopEnv.Contains("KDE", StringComparison.OrdinalIgnoreCase);
            IsGnome = desktopEnv.Contains("GNOME", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            IsKDE = false;
            IsGnome = false;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the system is running KDE desktop environment.
    /// This is only checked on Linux systems.
    /// </summary>
    internal static readonly bool IsKDE;

    /// <summary>
    /// Gets a value indicating whether the system is running GNOME desktop environment.
    /// This is only checked on Linux systems.
    /// </summary>
    internal static readonly bool IsGnome;
}