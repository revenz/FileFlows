using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace FileFlows.Node.Ui;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private MainWindow Window { get; init; }
    public MainWindowViewModel(MainWindow window)
    {
        this.Window = window;
    }
    
    /// <summary>
    /// Quits the application
    /// </summary>
    public void Quit() => Window.Quit();
    
    
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _ServerUrl = string.Empty;
    public string ServerUrl
    {
        get => _ServerUrl;
        set
        {
            if (_ServerUrl != value)
            {
                _ServerUrl = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServerUrl)));
            }
        }
    }
    private string _AccessToken = string.Empty;
    public string AccessToken
    {
        get => _AccessToken;
        set
        {
            if (_AccessToken != value)
            {
                _AccessToken = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccessToken)));
            }
        }
    }

    private bool _StartMinimized = false;

    public bool StartMinimized
    {
        get => _StartMinimized;
        set
        {
            if (_StartMinimized != value)
            {
                _StartMinimized = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartMinimized)));
            }
        }
    }
    
    public Result<bool> Validate()
    {
        if (string.IsNullOrWhiteSpace(ServerUrl))
            return Result<bool>.Fail("Server URL cannot be empty.");

        string url = ServerUrl.Trim();

        // If the URL does not start with http:// or https://, prepend http://
        if (!Regex.IsMatch(url, @"^https?://", RegexOptions.IgnoreCase))
            url = "http://" + url;

        // Extract the host (remove http:// or https://)
        string hostPart = url.Substring(url.IndexOf("://") + 3).TrimEnd('/');

        // If the host doesn't have a port and isn't an IP, add the default port :19200
        if (!Regex.IsMatch(hostPart, @"(:\d+)$") && !Regex.IsMatch(hostPart, @"^\d{1,3}(\.\d{1,3}){3}$"))
            url = url.TrimEnd('/') + ":19200/";

        // Ensure the URL has a trailing slash
        if (!url.EndsWith("/"))
            url += "/";

        // Regex pattern to match:
        // - localhost
        // - IPv4 (e.g., 192.168.1.10)
        // - Hostnames (e.g., "bob", "my-server")
        // - Domains (e.g., example.com)
        // - Must have a port unless it's an IP
        string pattern = @"^https?://([a-zA-Z0-9-]+|\d{1,3}(\.\d{1,3}){3})(:\d+)?/$";

        if (!Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase))
            return Result<bool>.Fail("Invalid URL format.");

        ServerUrl = url; // Save the corrected URL
        return true;
    }


}