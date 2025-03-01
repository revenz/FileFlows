using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Markup.Xaml.Styling;
using FileFlows.AvaloniaUi.Helpers;

namespace FileFlows.AvaloniaUi;

/// <summary>
/// Base application class for Avalonia UI.
/// Handles theme initialization, platform-specific styles, and system settings.
/// </summary>
public abstract partial class App : Application
{
    /// <summary>
    /// The theme in use
    /// </summary>
    private SimpleTheme _theme = new SimpleTheme();

    /// <summary>
    /// Initializes the application, loading styles, themes, and system settings.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        // Detect OS theme before UI initializes
        var initialTheme = Application.Current!.ActualThemeVariant;
        _theme = new SimpleTheme();
        Styles.Insert(0, _theme);

        // Get system text scaling factor
        double scale = SystemTextScale.GetSystemTextScalingFactor();
        // Set the base font size dynamically
        double baseFontSize = (OperatingSystem.IsMacOS() ? 14 : 16) * scale; // Scale base size dynamically
        Current.Resources["BaseFontSize"] = baseFontSize;
        Current.Resources["LargeFontSize"] = baseFontSize * 1.25f;

        // Set the OS accent color as a resource

        LoadPlatformSpecificStyles();
    }


    /// <summary>
    /// Loads platform-specific styles based on the current operating system.
    /// </summary>
    private void LoadPlatformSpecificStyles()
    {
        string stylePath = "avares://FileFlows.AvaloniaUI/Styles/Gnome.axaml"; // Fallback

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            stylePath = "avares://FileFlows.AvaloniaUI/Styles/Windows.axaml";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && SystemHelper.IsGnome)
            stylePath = "avares://FileFlows.AvaloniaUI/Styles/Gnome.axaml";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            stylePath = "avares://FileFlows.AvaloniaUI/Styles/Mac.axaml";

        Styles.Add(new StyleInclude(new Uri("avares://FileFlows.AvaloniaUI"))
        {
            Source = new Uri(stylePath)
        });
    }


    /// <summary>
    /// Determines an appropriate text color for contrast against the given background color.
    /// </summary>
    /// <param name="background">The background color.</param>
    /// <returns>The contrasting text color (either black or white).</returns>
    private Color GetContrastingTextColor(Color background)
    {
        double luminance = (0.299 * background.R + 0.587 * background.G + 0.114 * background.B) / 255;
        return luminance > 0.5 ? Colors.Black : Colors.White;
    }


    /// <summary>
    /// Called when the framework initialization is complete.
    /// Initializes the main window and applies the correct theme.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Create the main window here, but don't display it yet
            var mainWindow = CreateMainWindow();

            // Immediately apply the theme
            UpdateTheme(mainWindow.ActualThemeVariant);
            mainWindow.ActualThemeVariantChanged += (_, _) => { UpdateTheme(mainWindow.ActualThemeVariant); };

            // Check if the app should start minimized and hidden
            if (GetInitialStartMinimized())
            {
                // Ensure the window is created minimized and hidden before it's displayed
                mainWindow.WindowState = WindowState.Minimized;

                // Post to the UI thread after framework initialization is complete
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    // Hide the window immediately
                    mainWindow.Hide();
                    desktop.MainWindow = mainWindow;
                }, Avalonia.Threading.DispatcherPriority.ApplicationIdle);
            }
            else
            {
                desktop.MainWindow = mainWindow;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }


    /// <summary>
    /// Creates the main window for the application.
    /// </summary>
    /// <returns>A new instance of the main window.</returns>
    protected abstract Window CreateMainWindow();

    /// <summary>
    /// Retrieves the FileFlows server URL used to open a browser to this location
    /// </summary>
    /// <returns>The server URL as a string.</returns>
    protected abstract string GetServerUrl();

    /// <summary>
    /// Retrieves the logging directory
    /// </summary>
    /// <returns>The logging directory path.</returns>
    protected abstract string GetLoggingDirectory();

    /// <summary>
    /// Gets the initial state of start minimized
    /// </summary>
    /// <returns>the state</returns>
    protected abstract bool GetInitialStartMinimized();

    /// <summary>
    /// Opens the specified server URL in the default web browser.
    /// </summary>
    private void Open(object? sender, EventArgs e)
    {
        string url = GetServerUrl();
        if (string.IsNullOrWhiteSpace(url))
            return;

        if (OperatingSystem.IsWindows())
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", url);
        else
            Process.Start(new ProcessStartInfo("xdg-open", url));
    }

    /// <summary>
    /// The tray icon was clicked
    /// </summary>
    /// <param name="sender">the sender</param>
    /// <param name="e">the event</param>
    private void TrayIcon_OnClicked(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
        {
            if (desktop.MainWindow.WindowState == WindowState.Normal)
            {
                desktop.MainWindow.WindowState = WindowState.Minimized;
                desktop.MainWindow.Hide();
            }
            else
            {
                desktop.MainWindow.Show();
                desktop.MainWindow.WindowState = WindowState.Normal;
            }
        }
    }

    /// <summary>
    /// The shows the window
    /// </summary>
    /// <param name="sender">the sender</param>
    /// <param name="e">the event</param>
    private void ShowWindow(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: not null } desktop)
        {
            desktop.MainWindow.Show();
            desktop.MainWindow.WindowState = WindowState.Normal;
        }
    }


    /// <summary>
    /// Opens the logging directory in the host's file browser (Explorer, Finder, etc).
    /// </summary>
    private void Logs(object? sender, EventArgs e)
    {
        string path = GetLoggingDirectory();
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return;

        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo("explorer", $"/select,{path}") { CreateNoWindow = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", path);
        }
        else
        {
            Process.Start(new ProcessStartInfo("xdg-open", path));
        }
    }

    /// <summary>
    /// Shuts down the application.
    /// </summary>
    private void Quit(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }


    /// <summary>
    /// Updates the application theme dynamically based on the selected theme variant.
    /// </summary>
    /// <param name="variant">The selected theme variant.</param>
    private void UpdateTheme(ThemeVariant variant)
    {
        if (_theme != null)
            Styles.Remove(_theme);

        _theme = new SimpleTheme();
        Styles.Insert(0, _theme);

        var accentColor = SystemAccentColor.GetOsAccentColor();

        if (Current != null)
        {
            Current.Resources["SystemAccentColor"] = accentColor;
            Current.Resources["SystemAccentTextColor"] = GetContrastingTextColor(accentColor);

            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            bool isGnome = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && SystemHelper.IsGnome;

            if (variant == ThemeVariant.Dark)
            {
                if (isGnome)
                {
                    // GNOME Dark Theme Colors
                    Current.Resources["DefaultButtonBackground"] = Color.FromRgb(50, 50, 50);
                    Current.Resources["DefaultButtonForeground"] = Colors.White;
                    Current.Resources["DefaultButtonHover"] = Color.FromRgb(70, 70, 70);
                    Current.Resources["DefaultButtonPressed"] = Color.FromRgb(90, 90, 90);
                    Current.Resources["SystemAccentBackground"] = Color.FromArgb(20, accentColor.R, accentColor.G, accentColor.B);

                    
                    Current.Resources["DisabledBackground"] = Color.FromRgb(34, 34, 34);
                    Current.Resources["DisabledForeground"] = Color.FromRgb(200, 200, 200);

                    Current.Resources["TextBoxBackground"] = Color.FromRgb(54, 54, 54);
                    Current.Resources["TextBoxForeground"] = Colors.White;
                }
                else if (isWindows)
                {
                    // Windows Dark Theme Colors
                    Current.Resources["DefaultButtonBackground"] = Color.FromRgb(45, 45, 45);
                    Current.Resources["DefaultButtonForeground"] = Colors.White;
                    Current.Resources["DefaultButtonHover"] = Color.FromRgb(60, 60, 60);
                    Current.Resources["DefaultButtonPressed"] = Color.FromRgb(80, 80, 80);

                    Current.Resources["TextBoxBackground"] = Color.FromRgb(30, 30, 30);
                    Current.Resources["TextBoxForeground"] = Colors.White;
                }
                else if (isMac)
                {
                    // macOS Dark Theme Colors
                    Current.Resources["DefaultButtonBackground"] = Color.FromRgb(86, 84, 89);
                    Current.Resources["DefaultButtonForeground"] = Colors.White;
                    Current.Resources["DefaultButtonHover"] = Color.FromRgb(78, 78, 78);
                    Current.Resources["DefaultButtonPressed"] = Color.FromRgb(98, 98, 98);

                    Current.Resources["TextBoxBackground"] = Color.FromRgb(44, 44, 44);
                    Current.Resources["TextBoxForeground"] = Colors.White;
                }
                else
                {
                    // Default Dark Theme Colors
                    Current.Resources["DefaultButtonBackground"] = Color.FromRgb(55, 55, 55);
                    Current.Resources["DefaultButtonForeground"] = Colors.White;
                    Current.Resources["DefaultButtonHover"] = Color.FromRgb(75, 75, 75);
                    Current.Resources["DefaultButtonPressed"] = Color.FromRgb(95, 95, 95);

                    Current.Resources["TextBoxBackground"] = Color.FromRgb(35, 35, 35);
                    Current.Resources["TextBoxForeground"] = Colors.White;
                }
            }
            else
            {
                if (isGnome)
                {
                    // GNOME Light Theme Colors
                    Current.Resources["DefaultButtonBackground"] = Color.FromRgb(229, 229, 229);
                    Current.Resources["DefaultButtonForeground"] = Colors.Black;
                    Current.Resources["DefaultButtonHover"] = Color.FromRgb(213, 213, 213);
                    Current.Resources["DefaultButtonPressed"] = Color.FromRgb(197, 197, 197);
                    
                    Current.Resources["DisabledBackground"] = Color.FromRgb(200, 200, 200);
                    Current.Resources["DisabledForeground"] = Color.FromRgb(1, 1, 1);

                    Current.Resources["TextBoxBackground"] = Color.FromRgb(245, 245, 245);
                    Current.Resources["TextBoxForeground"] = Colors.Black;
                }
                else if (isWindows)
                {
                    // Windows Light Theme Colors
                    Current.Resources["DefaultButtonBackground"] = Color.FromRgb(240, 240, 240);
                    Current.Resources["DefaultButtonForeground"] = Colors.Black;
                    Current.Resources["DefaultButtonHover"] = Color.FromRgb(225, 225, 225);
                    Current.Resources["DefaultButtonPressed"] = Color.FromRgb(200, 200, 200);

                    Current.Resources["TextBoxBackground"] = Color.FromRgb(255, 255, 255);
                    Current.Resources["TextBoxForeground"] = Colors.Black;
                }
                else if (isMac)
                {
                    // macOS Light Theme Colors
                    Current.Resources["DefaultButtonBackground"] = Color.FromRgb(242, 242, 242);
                    Current.Resources["DefaultButtonForeground"] = Colors.Black;
                    Current.Resources["DefaultButtonHover"] = Color.FromRgb(226, 226, 226);
                    Current.Resources["DefaultButtonPressed"] = Color.FromRgb(210, 210, 210);

                    Current.Resources["TextBoxBackground"] = Color.FromRgb(250, 250, 250);
                    Current.Resources["TextBoxForeground"] = Colors.Black;
                }
                else
                {
                    // Default Light Theme Colors
                    Current.Resources["DefaultButtonBackground"] = Color.FromRgb(235, 235, 235);
                    Current.Resources["DefaultButtonForeground"] = Colors.Black;
                    Current.Resources["DefaultButtonHover"] = Color.FromRgb(220, 220, 220);
                    Current.Resources["DefaultButtonPressed"] = Color.FromRgb(200, 200, 200);

                    Current.Resources["TextBoxBackground"] = Color.FromRgb(245, 245, 245);
                    Current.Resources["TextBoxForeground"] = Colors.Black;
                }
            }
        }
    }
}