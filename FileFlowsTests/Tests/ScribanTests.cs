#if(DEBUG)
using System.IO;
using FileFlows.Client.Pages;
using FileFlows.FlowRunner.TemplateRenders;
using FileFlows.ServerShared.FileServices;

namespace FileFlowsTests.Tests;

[TestClass]
public class ScribanTests : TestBase
{

    [TestMethod]
    public void ScribanTest()
    {
        string text = @"
File: {{Variables.file.Orig.FullName}}
Extension: {{Variables.ext}}
{{if Variables.file.Orig.Size > 100000 -}}
    Size is greater than 100000.
    Size is {{Variables.file.Orig.Size}}
{{else -}}
    Size is not greater than 100000.
    Size is {{Variables.file.Orig.Size}}
{{- end -}}
";
        var tempFile = GetTempFileName();
        System.IO.File.WriteAllText(tempFile, string.Join("\n", Enumerable.Range(0, 10000).Select(x => Guid.NewGuid().ToString())));
        var args = new NodeParameters(tempFile, Logger, false, string.Empty, new LocalFileService(false));
        args.InitFile(tempFile);
        var renderer = new ScribanRenderer();
        var rendered = renderer.Render(args, text);
        Logger.ILog(rendered);
        Logger.ILog(new string('=', 30));
    }
    
    [TestMethod]
    public void ScribanTest2()
    {
        string text = @"
{{ difference = file.Size - file.Orig.Size }}
{{ percent = (difference / file.Orig.Size) * 100 | math.round 2 }}

Input File: {{ file.Orig.FullName }}
Output File: {{ file.FullName }}
Original Size: {{ file.Orig.Size | file_size }}
Final Size: {{ file.Size | file_size }}

{{- if difference < 0 }}
File grew in size: {{ difference | math.abs | file_size }}
{{ else }}
File shrunk in size by: {{ difference | file_size }} / {{ percent }}%
{{ end }}
".Trim();
        var tempFile = GetTempFileName();
        var random = new Random(DateTime.UtcNow.Millisecond);
        File.WriteAllText(tempFile, string.Join("\n", Enumerable.Range(0, random.Next(1000, 100000)).Select(x => Guid.NewGuid().ToString())));
        var args = new NodeParameters(tempFile, Logger, false, string.Empty, new LocalFileService(false));
        args.InitFile(tempFile);
        
        var tempFile2 = GetTempFileName();
        File.WriteAllText(tempFile2, string.Join("\n", Enumerable.Range(0, random.Next(1000, 100000)).Select(x => Guid.NewGuid().ToString())));
        args.SetWorkingFile(tempFile2);
        
        var renderer = new ScribanRenderer();
        var rendered = renderer.Render(args, text);
        Logger.ILog(rendered);
        Logger.ILog(new string('=', 30));
        
    }
    
    [TestMethod]
    public void ScribanTimeTests()
    {
        VariablesHelper.StartedAt = DateTime.Now.AddMinutes(-10);
        string text = @"
Time: {{ time.now }}
Time Processing: {{ time.processing }}
".Trim();
        var tempFile = GetTempFileName();
        var random = new Random(DateTime.UtcNow.Millisecond);
        File.WriteAllText(tempFile, string.Join("\n", Enumerable.Range(0, random.Next(1000, 100000)).Select(x => Guid.NewGuid().ToString())));
        var args = new NodeParameters(tempFile, Logger, false, string.Empty, new LocalFileService(false));
        args.InitFile(tempFile);
        
        var tempFile2 = GetTempFileName();
        File.WriteAllText(tempFile2, string.Join("\n", Enumerable.Range(0, random.Next(1000, 100000)).Select(x => Guid.NewGuid().ToString())));
        args.SetWorkingFile(tempFile2);
        
        var renderer = new ScribanRenderer();
        var rendered = renderer.Render(args, text);
        Logger.ILog(rendered);
        
        Assert.IsTrue(rendered.Contains("Time Processing: 00:10"));
    }

    [TestMethod]
    public void ScribanLibraryNameTest()
    {
        VariablesHelper.StartedAt = DateTime.Now.AddMinutes(-10);
        string text = @"
Library: {{ Library.Name }}
".Trim();
        var tempFile = GetTempFileName();
        var random = new Random(DateTime.UtcNow.Millisecond);
        File.WriteAllText(tempFile, string.Join("\n", Enumerable.Range(0, random.Next(1000, 100000)).Select(x => Guid.NewGuid().ToString())));
        var args = new NodeParameters(tempFile, Logger, false, string.Empty, new LocalFileService(false));
        args.InitFile(tempFile);
        args.Variables["Library.Name"] = "Test Library";
        
        var renderer = new ScribanRenderer();
        var rendered = renderer.Render(args, text);
        Logger.ILog(rendered);
        
        Assert.AreEqual("Library: Test Library", rendered);
    }
    
//     [TestMethod]
//     public void HandlebarTest()
//     {
//         string text = @"
// File: {{Variables.file.Orig.FullName}}
// Extension: {{Variables.ext}}
// ";
//         var tempFile = Path.GetTempFileName();
//         System.IO.File.WriteAllText(tempFile, string.Join("\n", Enumerable.Range(0, 10000).Select(x => Guid.NewGuid().ToString())));
//         var logger = new TestLogger();
//         var args = new NodeParameters(tempFile, logger, false, string.Empty, new LocalFileService());
//         args.InitFile(tempFile);
//         var renderer = new HandlebarsRenderer();
//         var rendered = renderer.Render(args, text);
//         var log = logger.ToString();
//     }
}
#endif