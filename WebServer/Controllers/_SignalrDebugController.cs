using System.Text;

#if(DEBUG)
namespace FileFlows.WebServer.Controllers;

[Route("debug-info")]
public class _SignalrDebugController : Controller
{
    [HttpGet]
    public IActionResult GetHtmlOverview()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("""
<html>
    <head>
    <meta http-equiv="refresh" content="5">
    <style>
        table {
        width: 100%;
        border-collapse: collapse;
        margin: 20px 0;
        font-size: 16px;
        }

        th, td {
        border: 1px solid #ddd;
        padding: 10px;
        text-align: left;
        }

        th {
        background-color: #007bff;
        color: white;
        font-weight: bold;
        }

        tr:nth-child(even) {
        background-color: #f2f2f2;
        }

        tr:hover {
        background-color: #ddd;
        }

        </style>
    </head>
    <body>
""");
        
        builder.AppendLine(GetNodeOverview());
        builder.AppendLine(GetQueuedFiles());
        
        builder.AppendLine("</body></html>");
        return Content(builder.ToString(), "text/html");
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
                                 <td>{node.NodeUid}</td>
                                 <td>{node.ConfigRevision}</td>
                                 <td>{node.ConnectionId}</td>
                                 <td>{node.ActiveRunners?.Count} / {node.MaxRunners}</td>
                             </tr>
                             """);
        }

        html.AppendLine("</tbody></table>");
        return html.ToString();
    }
    
}
#endif