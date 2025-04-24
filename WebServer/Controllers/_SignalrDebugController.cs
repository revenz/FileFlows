#if(DEBUG)

using System.Text;
using FileFlows.Services.FileProcessing;
using Humanizer;
using Swashbuckle.AspNetCore.Annotations;

namespace FileFlows.WebServer.Controllers;

[Route("debug-info")]
public class _SignalrDebugController : Controller
{
    [HttpGet]
    [SwaggerIgnore]
    public async Task<IActionResult> GetHtmlOverview()
    {
        #if(DEBUG == false)
        if (Globals.FileFlowsDotComUrl == null)
            return Forbid();
        if(Globals.FileFlowsDotComUrl.Contains("pre") == false && 
           Globals.FileFlowsDotComUrl.Contains("10.") == false)
            return Forbid();
        #endif 

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
        builder.AppendLine($"<h1>FileFlows v{Globals.Version}</h1>");
        builder.AppendLine(await GetTopInfo());
        builder.AppendLine(GetRunners());
        builder.AppendLine(GetNodeOverview());
        builder.AppendLine(GetFileStatusCounts());
        builder.AppendLine(GetQueuedFiles());
        builder.AppendLine(GetOnHold());
        
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
        var service = ServiceLoader.Load<FileSorterService>();
        var files = service.GetData(FileStatus.Unprocessed);
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

        return html.ToString();
    }

     private string GetOnHold()
     {
         var service = ServiceLoader.Load<FileSorterService>();
         var files = service.GetData(FileStatus.OnHold);
         StringBuilder html = new ($"""
                                    <h2>On Hold</h2>
                                    <table>
                                        <tbody>
                                            <thead>
                                                <tr>
                                                    <th>UID</th>
                                                    <th>Until</th>
                                                    <th>File ({files.Count})</th>
                                                </tr>
                                            </thead>
                                        <tbody>
                                    """);
         foreach (var file in files)
         {
             html.AppendLine($"""
                              <tr>
                                  <td>{file.Uid}</td>
                                  <td>{file.HoldUntil.Subtract(DateTime.UtcNow).Humanize()}</td>
                                  <td>{file.Name}</td>
                              </tr>
                              """);
         }

         html.AppendLine("</tbody></table>");

         return html.ToString();
     }
     
     private string GetFileStatusCounts()
     {
         var service = ServiceLoader.Load<FileSorterService>();
         var files = service.GetStatuses();
         StringBuilder html = new ($"""
                                    <table>
                                        <tbody>
                                            <thead>
                                                <tr>
                                                    <th>State</th>
                                                    <th>Files ({files.Count})</th>
                                                </tr>
                                            </thead>
                                        <tbody>
                                    """);
         foreach (var status in files)
         {
             html.AppendLine($"""
                              <tr>
                                  <td>{status.Status}</td>
                                  <td>{status.Count}</td>
                              </tr>
                              """);
         }

         html.AppendLine("</tbody></table>");

         return $"<h2>File Status Counts</h2>{html}";
     }
     

     private string? GetRunners()
     {
         var service = ServiceLoader.Load<NodeService>();
         var runners = service.GetRunners();
         var processing = ServiceLoader.Load<FileSorterService>().GetProcessing();
         StringBuilder html = new ("""
                                   <table>
                                       <tbody>
                                           <thead>
                                               <tr>
                                                   <th>Uid</th>
                                                   <th>File</th>
                                                   <th>Node</th>
                                               </tr>
                                           </thead>
                                       <tbody>
                                   """);
         
         foreach (var p in processing)
         {
             var runner = runners.FirstOrDefault(x => x.LibraryFile.Uid == p.Uid);
             if (runner != null)
                 runners.Remove(runner);
             
             html.AppendLine($$"""
                               <tr>
                                   <td>{{p.Uid}}</td>
                                   <td>{{runner?.LibraryFile?.Name?.EmptyAsNull() ?? p.DisplayName}}</td>
                                   <td>{{runner?.NodeName?.EmptyAsNull() ?? "No Runner"}}</td>
                               </tr>
                               """);
         }
         foreach (var runner in runners)
         {
             html.AppendLine($$"""
                               <tr>
                                   <td>[NOT Tracked] {{runner.LibraryFile.Uid}}</td>
                                   <td>{{runner?.LibraryFile?.Name?.EmptyAsNull()}}</td>
                                   <td>{{runner?.NodeName?.EmptyAsNull() ?? "No Runner"}}</td>
                               </tr>
                               """);
         }

         html.AppendLine("</tbody></table>");
         return $"<h2>Processing Files</h2>{html}";
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
                <th>Max Runners</th>
                <th>Runners</th>
            </tr>
        </thead>
    <tbody>
""");
        foreach (var node in nodes)
        {
            html.AppendLine($$"""
                              <tr>
                                  <td>{{node.Node.Name}}</td>
                                  <td>{{node.ConfigRevision}}</td>
                                  <td>{{node.ConnectionId}}</td>
                                  <td>{{node.Node.FlowRunners}}</td>
                                  <td style="white-space:pre">{{string.Join("\n", (node.Runners ?? []).Select(x => x.Value.LibraryFile.Name.ToString()))}}</td>
                              </tr>
                              """);
        }

        html.AppendLine("</tbody></table>");
        return html.ToString();
    }
    
}

#endif