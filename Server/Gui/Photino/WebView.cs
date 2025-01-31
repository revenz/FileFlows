using FileFlows.WebServer;
using PhotinoNET;

namespace FileFlows.Server.Gui.Photino;

/// <summary>
/// Photino web view
/// </summary>
public class WebView
{
    /// <summary>
    /// the photino window instance
    /// </summary>
    private PhotinoWindow window;
    
    /// <summary>
    /// Gets or sets if the web view has opened 
    /// </summary>
    public bool Opened { get; private set; }

    /// <summary>
    /// Constructs a web view
    /// </summary>
    public WebView()
    {
        WebServerApp.OnStatusUpdate += WebServer_StatusUpdate;
    }

    /// <summary>
    /// Event called when the web server sends a status update
    /// </summary>
    /// <param name="state">the state</param>
    /// <param name="message">the message</param>
    /// <param name="url">the URL of the web server</param>
    private void WebServer_StatusUpdate(WebServerState state, string message, string url)
    {
        if(OperatingSystem.IsWindows())
            Thread.Sleep(500);
        
        try
        {
            if (state == WebServerState.Error)
                ShowError(message);
            if (state == WebServerState.Listening)
            {
                Thread.Sleep(5000);
                window.LoadRawString(GetLoadingHtml(message));

                // Parse the URL
                Uri uri = new Uri(url);
                string protocol = uri.Scheme;
                int port = uri.Port;
                if (port > 0)
                    url = $"{protocol}://{Environment.MachineName.ToLowerInvariant()}:{port}/";
                else
                    url = $"{protocol}://{Environment.MachineName.ToLowerInvariant()}/";

#if(DEBUG)
                url = "http://localhost:5276/";
#endif

                LoadIFrame(url);
            }

            if (state == WebServerState.Starting)
            {
                if (window == null)
                    Thread.Sleep(100);
                if (window != null)
                    window.LoadRawString(GetLoadingHtml(message));
            }
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("WebServer_StatusUpdate error: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }

    }
    
    /// <summary>
    /// Opens a web view at the given URL
    /// </summary>
    public void Open()
    {
        string folderPrefix = "";
#if (DEBUG)
        folderPrefix = "../Client/";
#endif

        var iconFile = folderPrefix + "wwwroot/icon" + (PhotinoWindow.IsWindowsPlatform ? ".ico" : ".png");

        // Creating a new PhotinoWindow instance with the fluent API
        window = new PhotinoWindow()
            .SetTitle("FileFlows")
            // Resize to a percentage of the main monitor work area
            .SetUseOsDefaultSize(false)
            .SetSize(new System.Drawing.Size(1600, 1080))
            .Center()
            .SetIgnoreCertificateErrorsEnabled(true)
            //.SetGrantBrowserPermissions(true)
            .SetChromeless(false)
            .SetIconFile(iconFile)
            .SetResizable(true)
            .SetJavascriptClipboardAccessEnabled(true)
            .LoadRawString(GetLoadingHtml());
        window.WindowCreated += (sender, args) =>
        {
            Opened = true;
        }; 

        window.WaitForClose(); // Starts the application event loop
    }

    private string GetLoadingHtml(string message = "")
        => $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Loading Page</title>
    <style>
        body {{
            --base-darkest-rgb:7, 7, 7;
            --base-darkest:rgb(7, 7, 7);
            background-color: var(--base-darkest);
            color: #fff;
            font-size:14px;
            font-family: Arial, sans-serif;
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
        }}

        svg {{
            height:3rem;
        }}

        .message {{
            display:block;
            margin-top:3rem;
        }}  
        
        .version {{
            position:absolute;
            bottom:3rem;
        }}
    </style>
</head>
<body>

<div id=""loading"">
    <svg xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" version=""1.1"" width=""520"" height=""105"" viewBox=""0 0 315.15 63.64"" xml:space=""preserve"" fill=""#ff0090"">
<g transform=""matrix(0.86 0 0 0.97 36.11 32.98)"" >
	<path style=""stroke: none; stroke-width: 1; stroke-dasharray: none; stroke-linecap: butt; stroke-dashoffset: 0; stroke-linejoin: miter; stroke-miterlimit: 4; fill-rule: nonzero; opacity: 1;""  transform="" translate(-32, -31.95)"" d=""M 55.4 43.4 h -7.2 c -3.3 0 -6.2 2.2 -7.1 5.3 H 18.9 c -4.1 0 -7.5 -3.4 -7.5 -7.5 s 3.4 -7.5 7.5 -7.5 h 26.2 c 6.1 0 11 -4.9 11 -11 c 0 -6.1 -4.9 -11 -11 -11 H 23.2 v -0.5 c 0 -4.1 -3.3 -7.4 -7.4 -7.4 H 8.6 c -4.1 0 -7.4 3.3 -7.4 7.4 v 1.9 c 0 4.1 3.3 7.4 7.4 7.4 h 7.2 c 3.3 0 6.2 -2.2 7.1 -5.3 h 22.2 c 4.1 0 7.5 3.4 7.5 7.5 s -3.4 7.5 -7.5 7.5 H 18.9 c -6.1 0 -11 4.9 -11 11 s 4.9 11 11 11 h 21.9 v 0.5 c 0 4.1 3.3 7.4 7.4 7.4 h 7.2 c 4.1 0 7.4 -3.3 7.4 -7.4 v -1.9 C 62.8 46.7 59.4 43.4 55.4 43.4 z M 19.7 13.2 c 0 2.1 -1.7 3.9 -3.9 3.9 H 8.6 c -2.1 0 -3.9 -1.7 -3.9 -3.9 v -1.9 c 0 -2.1 1.7 -3.9 3.9 -3.9 h 7.2 c 2.1 0 3.9 1.7 3.9 3.9 V 13.2 z M 59.3 52.7 c 0 2.1 -1.7 3.9 -3.9 3.9 h -7.2 c -2.1 0 -3.9 -1.7 -3.9 -3.9 v -1.9 c 0 -2.1 1.7 -3.9 3.9 -3.9 h 7.2 c 2.1 0 3.9 1.7 3.9 3.9 V 52.7 z"" stroke-linecap=""round"" />
</g>
<g transform=""matrix(1 0 0 1 503 27.79)"" style=""""  >
		<text xml:space=""preserve"" font-family=""'Open Sans', sans-serif"" font-size=""18"" font-style=""normal"" font-weight=""normal"" style=""stroke: none; stroke-width: 1; stroke-dasharray: none; stroke-linecap: butt; stroke-dashoffset: 0; stroke-linejoin: miter; stroke-miterlimit: 4; fill-rule: nonzero; opacity: 1; white-space: pre;"" ><tspan x=""-432.45"" y=""0.31"" style=""font-size: 1px; font-style: italic; font-weight: bold; "">FileFlows</tspan></text>
</g>
<g transform=""matrix(1 0 0 1 283.05 35.03)"" style=""""  >
		<text xml:space=""preserve"" font-family=""'Open Sans', sans-serif"" font-size=""50"" font-style=""normal"" font-weight=""normal"" style=""stroke: none; stroke-width: 1; stroke-dasharray: none; stroke-linecap: butt; stroke-dashoffset: 0; stroke-linejoin: miter; stroke-miterlimit: 4; fill-rule: nonzero; opacity: 1; white-space: pre;"" ><tspan x=""-214.5"" y=""16.02"" style=""font-size: 51px; font-style: italic; font-weight: bold; "">FileFlows</tspan></text>
</g>
</svg>
</div>

<div class=""message"">{message ?? string.Empty}</div>

<div class=""version"">
    {Globals.Version}
</div>
</body>
</html>
";

    private void ShowError(string error)
    {
        try
        {
            window.LoadRawString($@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Error Page</title>
<style>
    body {{
        margin: 0;
        padding: 0;
        background-color: black;
        color: white;
        font-family: Arial, sans-serif;
        display: flex;
        justify-content: center;
        align-items: center;
        height: 100vh;
    }}

    .error-message {{
        text-align: center;
    }}
</style>
</head>
<body>
    <div class=""error-message"">
        <h1>Error!</h1>
        <p>{error}</p>
    </div>
</body>
</html>
");
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed setting error in webview: " + ex.Message + Environment.NewLine +
                                 ex.StackTrace);
        }
    }

    private void LoadIFrame(string url)
    {
        try
        {
            window//.SetContextMenuEnabled(false)
                .LoadRawString($@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>FileFlows</title>
    <style>
        html {{
            font-size:14px;
        }}
        body {{
            margin: 0;
            padding: 0;
            font-family: Arial, sans-serif;
            background-color: #000;
            color: #fff;
            display:flex;
            flex-direction:column;
            height:100vh;
            width:100vw;
            overflow:hidden;
        }}

        .address-bar {{
            display: flex;
            align-items: center;
            justify-content: center;
            padding:0.5rem 7rem;
            border-bottom:2px solid rgb(30,30,30);
        }}

        .address-bar span {{
            background-color: #222;
            padding: 0.25rem;
            border-radius: 1rem;
            flex-grow: 1;
            text-align: center;
        }}

        .refresh-button {{
            background-color: #444;
            color: #fff;
            width: 1.5rem;
            height: 1.5rem;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            cursor: pointer;
            margin-right: 0.25rem;
        }}

        .refresh-button:hover {{
            background-color: #555;
        }}

        .refresh-icon {{
            width: 1rem;
            height: 1rem;
            fill: currentColor;
        }}

        .iframe-container {{
            flex-grow:1;
        }}

        .iframe-container iframe {{
            width: 100%;
            height: 100%;
            border: none;
        }}
    </style>
</head>
<body>

<div class=""address-bar"">
<div class=""refresh-button"" onclick=""refreshPage()"">

    <svg class=""refresh-icon"" xmlns=""http://www.w3.org/2000/svg""  viewBox=""0 0 30 30"" width=""30px"" height=""30px""><path d=""M 15 3 C 12.031398 3 9.3028202 4.0834384 7.2070312 5.875 A 1.0001 1.0001 0 1 0 8.5058594 7.3945312 C 10.25407 5.9000929 12.516602 5 15 5 C 20.19656 5 24.450989 8.9379267 24.951172 14 L 22 14 L 26 20 L 30 14 L 26.949219 14 C 26.437925 7.8516588 21.277839 3 15 3 z M 4 10 L 0 16 L 3.0507812 16 C 3.562075 22.148341 8.7221607 27 15 27 C 17.968602 27 20.69718 25.916562 22.792969 24.125 A 1.0001 1.0001 0 1 0 21.494141 22.605469 C 19.74593 24.099907 17.483398 25 15 25 C 9.80344 25 5.5490109 21.062074 5.0488281 16 L 8 16 L 4 10 z""/></svg>
    
</div>
<span>{url}</span>
</div>

<div class=""iframe-container"">
<iframe src=""{url}""></iframe>
</div>

<script>
    function refreshPage() {{
        document.querySelector('iframe').src = document.querySelector('iframe').src;
    }}
</script>

</body>
</html>
");
        }
        catch (Exception ex)
        {
            Logger.Instance.ELog("Failed setting iframe content in webview: " + ex.Message + Environment.NewLine +
                                 ex.StackTrace);
        }
    }
}