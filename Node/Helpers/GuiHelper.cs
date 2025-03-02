using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using FileFlows.Node.Ui;

namespace FileFlows.Node.Helpers;

/// <summary>
/// Provides helper methods to show messages in a cross-platform manner.
/// Supports Windows, macOS, and Linux.
/// </summary>
public static class GuiHelper
{
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, int options);

    /// <summary>
    /// Displays a message box with a given title and message.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="message">The message content.</param>
    public static void ShowMessage(string title, string message)
    { 
        if (OperatingSystem.IsWindows())
        {
            MessageBox(IntPtr.Zero, message, title, 0); // 0 = OK button only
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            ShowMessageMac(title, message);
        }
        else{
            ShowMessageLinux(title, message);
        }
    }

    /// <summary>
    /// Displays a native macOS message box using AppleScript.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="message">The message content.</param>
    private static void ShowMessageMac(string title, string message)
    {
        string escapedMessage = message.Replace("\"", "\\\"").Replace("\n", " ");
        string escapedTitle = title.Replace("\"", "\\\"");

        Process.Start("osascript", $"-e \"display dialog \\\"{escapedMessage}\\\" buttons {{\\\"OK\\\"}} default button \\\"OK\\\" with title \\\"{escapedTitle}\\\"\"");
    }

    /// <summary>
    /// Displays a message box on Linux using either Zenity or KDialog.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="message">The message content.</param>
    private static void ShowMessageLinux(string title, string message)
    {
        if (IsCommandAvailable("zenity"))
        {
            Process.Start("zenity", $"--info --title=\"{title}\" --text=\"{message}\"");
        }
        else if (IsCommandAvailable("kdialog"))
        {
            Process.Start("kdialog", $"--msgbox \"{message}\" --title \"{title}\"");
        }
        else
        {
            Console.WriteLine("No supported dialog tool found on Linux (Zenity/KDialog required).");
        }
    }

    /// <summary>
    /// Checks if a specific command is available on the system.
    /// </summary>
    /// <param name="command">The command to check (e.g., "zenity" or "kdialog").</param>
    /// <returns>True if the command exists, otherwise false.</returns>
    private static bool IsCommandAvailable(string command)
    {
        try
        {
            ProcessStartInfo psi = new()
            {
                FileName = "which",
                Arguments = command,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            return process?.WaitForExit(1000) == true && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
