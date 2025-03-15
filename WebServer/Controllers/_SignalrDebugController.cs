using System.Text;
using FileFlows.Services.FileProcessing;

namespace FileFlows.WebServer.Controllers;

[Route("debug-info")]
public class _SignalrDebugController : Controller
{
    [HttpGet]
    public async Task<IActionResult> GetHtmlOverview()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("""
<html>
    <head>
    <meta http-equiv="refresh" content="5">
    <style>
        body {
            background:#222;
            color:#fff;
        }
        table {
        width: 100%;
        border-collapse: collapse;
        margin: 20px 0;
        font-size: 16px;
        }

        th, td {
        border: 1px solid #333;
        padding: 10px;
        text-align: left;
        }

        th {
        background-color: #3c3c3c;
        color: white;
        font-weight: bold;
        }

        tr:nth-child(even) {
        background-color: #2c2c2c;
        }

        tr:hover {
        background-color: #444;
        }

        </style>
    </head>
    <body>
""");
        
        builder.AppendLine(await GetTopInfo());
        builder.AppendLine(GetNodeOverview());
        builder.AppendLine(GetQueuedFiles());
        
        builder.AppendLine("</body></html>");
        return Content(builder.ToString(), "text/html");
    }

    private async Task<string> GetTopInfo()
    {
        int config = await ServiceLoader.Load<ISettingsService>().GetCurrentConfigurationRevision();
        StringBuilder html = new ($"""
                                   <div>
                                       <span>Current Config</span>
                                       <span>{config}</span>
                                    <div>
                                   """);
        return html.ToString();
    }

    private string GetQueuedFiles()
    {
        var service = ServiceLoader.Load<FileQueueService>();
        var files = service.PeekList();
        StringBuilder html = new ($"""
                                  <table>
                                      <tbody>
                                          <thead>
                                              <tr>
                                                  <th>UID</th>
                                                  <th>Files ({files.Count})</th>
                                              </tr>
                                          </thead>
                                      <tbody>
                                  """);
        foreach (var file in files)
        {
            html.AppendLine($"""
                             <tr>
                                 <td>{file.Uid}</td>
                                 <td>{file.Name}</td>
                             </tr>
                             """);
        }

        html.AppendLine("</tbody></table>");

        return $"<span>Test File: {(FileDispatcher.TestFile?.Name ?? "None")}" + html;
    }

    private string GetNodeOverview()
    {
        var service = ServiceLoader.Load<NodeService>();
        var nodes = service.GetOnlineNodes();
     StringBuilder html = new ("""
<table>
    <tbody>
        <thead>
            <tr>
                <th>UID</th>
                <th>Config</th>
                <th>ConnID</th>
                <th>Runners</th>
            </tr>
        </thead>
    <tbody>
""");
        foreach (var node in nodes)
        {
            html.AppendLine($"""
                             <tr>
                                 <td>{node.Node.Name}</td>
                                 <td>{node.ConfigRevision}</td>
                                 <td>{node.ConnectionId}</td>
                                 <td>{node.ActiveRunners?.Count} / {node.Node.FlowRunners}</td>
                             </tr>
                             """);
        }

        html.AppendLine("</tbody></table>");
        return html.ToString();
    }
    
}