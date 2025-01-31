using FileFlows.Plugin.Services;
using FileFlows.ServerShared.FileServices;
using Moq;

namespace FileFlowsTests.Tests.ScriptTests;

[TestClass]
public class FunctionTests : TestBase
{
    FileFlows.Plugin.NodeParameters Args;

    protected Mock<IFileService> MockFileService = new();

    private string TempFile;

    protected override void TestStarting()
    {
        Args = new FileFlows.Plugin.NodeParameters(@"c:\test\testfile.mkv", Logger, false, string.Empty, MockFileService.Object);
        Args.ScriptExecutor = new ScriptExecutor();
        TempFile = System.IO.Path.Combine(TempPath, Guid.NewGuid() + ".txt");
        System.IO.Directory.CreateDirectory(TempPath);
        System.IO.File.WriteAllText(TempFile, "this is a test file");
    }
    
    [TestMethod]
    public void Function_NoCode()
    {
        Function pm = new Function();
        pm.Code = null;
        var result = pm.Execute(Args);
        Assert.AreEqual(-1, result);

        Function pm2 = new Function();
        pm2.Code = string.Empty;
        result = pm2.Execute(Args);
        Assert.AreEqual(-1, result);
    }

    [TestMethod]
    public void Function_BadCode()
    {
        Function pm = new Function();
        pm.Code = "let x = {";
        var result = pm.Execute(Args);
        Assert.AreEqual(-1, result);
    }

    [TestMethod]
    public void Function_Basic_ReturnInts()
    {
        for (int i = 0; i < 10; i++)
        {
            Function pm = new Function();
            pm.Code = "return " + i;
            var result = pm.Execute(Args);
            Assert.AreEqual(i, result);
        }
    }


    [TestMethod]
    public void Function_UseVariables()
    {
        Function pm = new Function();
        var args = new FileFlows.Plugin.NodeParameters(@"c:\test\sdfsdfdsvfdcxdsf.mkv", Logger, false, string.Empty, MockFileService.Object);
        args.ScriptExecutor = new ScriptExecutor();
        args.Variables = new Dictionary<string, object>
        {
            { "movie.Title", "Ghostbusters" },
            { "movie.Year", 1984 }
        };
        pm.Code = @"
if(Variables['movie.Year'] == 1984) return 2;
return 0";
        var result = pm.Execute(args);
        Assert.AreEqual(2, result);
    }
    [TestMethod]
    public void Function_UseVariables_2()
    {
        Function pm = new Function();
        var args = new FileFlows.Plugin.NodeParameters(@"c:\test\sdfsdfdsvfdcxdsf.mkv", Logger, false, string.Empty, MockFileService.Object);
        args.ScriptExecutor = new ScriptExecutor();
        args.Variables = new Dictionary<string, object>
        {
            { "movie.Title", "Ghostbusters" },
            { "movie.Year", 1984 }
        };
        pm.Code = @"
if(Variables['movie.Year'] == 2000) return 2;
return 0";
        var result = pm.Execute(args);
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void Function_UseVariables_DotNotation()
    {
        Function pm = new Function();
        var args = new FileFlows.Plugin.NodeParameters(@"c:\test\sdfsdfdsvfdcxdsf.mkv", Logger, false, string.Empty, MockFileService.Object);
        args.ScriptExecutor = new ScriptExecutor();
        args.Variables = new Dictionary<string, object>
        {
            { "movie.Title", "Ghostbusters" },
            { "movie.Year", 1984 }
        };
        pm.Code = @"
if(Variables.movie.Year == 1984) return 2;
return 0";
        var result = pm.Execute(args);
        Assert.AreEqual(2, result);
    }

    [TestMethod]
    public void Function_VariableUpdate()
    {
        Function pm = new Function();
        var args = new FileFlows.Plugin.NodeParameters(@"c:\test\sdfsdfdsvfdcxdsf.mkv", Logger, false, string.Empty, MockFileService.Object);
        args.ScriptExecutor = new ScriptExecutor();
        args.Variables = new Dictionary<string, object>
        {
            { "movie.Title", "Ghostbusters" },
            { "movie.Year", 1984 }
        };
        pm.Code = @"
Variables.NewItem = 1234;
Variables.movie.Year = 2001;
return 0";
        var result = pm.Execute(args);
        Assert.IsTrue(args.Variables.ContainsKey("NewItem"));
        Assert.AreEqual(1234d, args.Variables["NewItem"]);
        Assert.AreEqual(2001d, args.Variables["movie.Year"]);
    }


    [TestMethod]
    public void Function_UseVariables_Date()
    {
        
        // Set the creation date to 2020-03-01
        DateTime specificDate = new DateTime(2020, 3, 1);
        System.IO.Directory.SetCreationTime(TempPath, specificDate);
        System.IO.Directory.SetLastWriteTime(TempPath, specificDate);
        System.IO.Directory.SetLastAccessTime(TempPath, specificDate);

        Function pm = new Function();
        var args = new NodeParameters(TempPath, Logger, false, string.Empty, MockFileService.Object);
        args.IsDirectory = true;
        args.InitFile(TempPath);
        args.ScriptExecutor = new ScriptExecutor();
        // args.Variables = new Dictionary<string, object>
        // {
        //     { "folder.Date", new DateTime(2020, 03, 01) }
        // };
        pm.Code = @"
Logger.ILog('Year: ' + Variables.folder.Date.Year); 
if(Variables.folder.Date.Year === 2020) return 1;
return 2";
        var result = pm.Execute(args);
        Assert.AreEqual(1, result);
    }
    
    [TestMethod]
    public void Function_UseVariables_MultipelDot()
    {
        Function pm = new Function();
        var args = new FileFlows.Plugin.NodeParameters(@"c:\test\sdfsdfdsvfdcxdsf.mkv", Logger, false, string.Empty, MockFileService.Object);
        args.ScriptExecutor = new ScriptExecutor();
        args.Variables = new Dictionary<string, object>
        {
            { "folder.Date.Year", 2020 }
        };
        pm.Code = @"
if(Variables.folder.Date.Year === 2020) return 1;
return 2";
        var result = pm.Execute(args);
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void Function_Flow_SetParameter()
    {
        Function pm = new Function();
        var args = new FileFlows.Plugin.NodeParameters(@"c:\test\sdfsdfdsvfdcxdsf.mkv", Logger, false, string.Empty, MockFileService.Object);
        args.ScriptExecutor = new ScriptExecutor();
        Assert.IsFalse(args.Parameters.ContainsKey("batman"));
        pm.Code = @"
Flow.SetParameter('batman', 1989);
return 1";
        var result = pm.Execute(args);
        Assert.AreEqual(1, result);
        Assert.IsTrue(args.Parameters.ContainsKey("batman"));
        Assert.AreEqual(args.Parameters["batman"].ToString(), "1989");
    }

    [TestMethod]
    public void Function_Flow_GetDirectorySize()
    {
        Function pm = new Function();
        var args = new FileFlows.Plugin.NodeParameters(TempFile, Logger, false, string.Empty, new LocalFileService(false));
        args.ScriptExecutor = new ScriptExecutor();
        pm.Code = $"return Flow.GetDirectorySize('{TempPath}');";
        var result = pm.Execute(args);
        Assert.IsTrue(result > 0);
    }



    [TestMethod]
    public void Function_Log()
    {
        Function pm = new Function();
        var args = new FileFlows.Plugin.NodeParameters(@"D:\videos\unprocessed\The IT Crowd - 2x04 - The Dinner Party - No English.mkv", Logger, false, string.Empty, MockFileService.Object);
        args.ScriptExecutor = new ScriptExecutor();
        pm.Code = @"
Logger.ILog('My Message');
return 2;
        ; ";
        var result = pm.Execute(args);
        Assert.AreEqual(2, result);
    }



    [TestMethod]
    public void Function_Flow_NullabeVI()
    {
        Function pm = new Function();
        var args = new FileFlows.Plugin.NodeParameters(@"c:\test\sdfsdfdsvfdcxdsf.mkv", Logger, false, string.Empty, MockFileService.Object);
        args.ScriptExecutor = new ScriptExecutor();

        foreach(var kv in new Dictionary<string, object>()
        {
            { "vi.Video.Codec", "hevc" },
            { "vi.Audio.Codec", "ac3" },
            { "vi.Audio.Codecs", "ac3,aac"},
            { "vi.Audio.Language", "eng" },
            { "vi.Audio.Languages", "eng, mao" },
            { "vi.Resolution", "1080p" },
            { "vi.Duration", 1800 },
            { "vi.VideoInfo", new 
                {
                    Bitrate = 10_000_000,
                    VideoStreams = new List<object> {
                        new { Width = 1920, Height = 1080 }
                    }
                }
            },
            { "vi.Width", 1920 },
            { "vi.Height", 1080 },
        })
        {
            args.Variables.Add(kv.Key, kv.Value);
        };

        pm.Code = @"
// get the first video stream, likely the only one
let video = Variables.vi?.VideoInfo?.VideoStreams[0];
if (!video)
return -1; // no video streams detected

if (video.Width > 1920)
{
// down scale to 1920 and encodes using NVIDIA
// then add a 'Video Encode' node and in that node 
// set 
// 'Video Codec' to 'hevc'
// 'Video Codec Parameters' to '{EncodingParameters}'
Logger.ILog(`Need to downscale from ${video.Width}x${video.Height}`);
Variables.EncodingParameters = '-vf scale=1920:-2:flags=lanczos -c:v hevc_nvenc -preset hq -crf 23'
return 1;
}

Logger.ILog('Do not need to downscale');
return 2;";
        var result = pm.Execute(args);
        Assert.IsTrue(result > 0);
    }


    [TestMethod]
    public void Function_Flow_NullabeVI_2()
    {
        Function pm = new Function();
        var args = new FileFlows.Plugin.NodeParameters(@"c:\test\sdfsdfdsvfdcxdsf.mkv", Logger, false, string.Empty, MockFileService.Object);
        args.ScriptExecutor = new ScriptExecutor();

        foreach (var kv in new Dictionary<string, object>()
        {
            { "vi.Video.Codec", "hevc" },
            { "vi.Audio.Codec", "ac3" },
            { "vi.Audio.Codecs", "ac3,aac"},
            { "vi.Audio.Language", "eng" },
            { "vi.Audio.Languages", "eng, mao" },
            { "vi.Resolution", "1080p" },
            { "vi.Duration", 1800 },
            { "vi.VideoInfo", new
                {
                    Bitrate = 10_000_000,
                    VideoStreams = new List<object> {
                        new { Width = 1920, Height = 1080 }
                    },
                    AudioStreams = new List<object> {
                        new { Bitrate = 1_000 }
                    }
                }
            },
            { "vi.Width", 1920 },
            { "vi.Height", 1080 },
        })
        {
            args.Variables.Add(kv.Key, kv.Value);
        };

        pm.Code = @"
// check if the bitrate for a video is over a certain amount
let MAX_BITRATE = 3_000_000; // bitrate is 3,000 KBps

let vi = Variables.vi?.VideoInfo;
if(!vi)
return -1; // no video information found

// get the video stream
let bitrate = vi.VideoStreams[0]?.Bitrate;

if(!bitrate)
{
// video stream doesn't have bitrate information
// need to use the overall bitrate
let overall = vi.Bitrate;
if(!overall)
	return 0; // couldn't get overall bitrate either

// overall bitrate includes all audio streams, so we try and subtrack those
let calculated = overall;
if(vi.AudioStreams?.length) // check there are audio streams
{
	for(let audio of vi.AudioStreams)
	{
		if(audio.Bitrate > 0)
			calculated -= audio.Bitrate;
		else{
			// audio doesn't have bitrate either, so we just subtract 5% of the original bitrate
			// this is a guess, but it should get us close
			calculated -= (overall * 0.05);
		}
	}
}
bitrate = calculated;
}

// check if the bitrate is over the maximum bitrate
if(bitrate > MAX_BITRATE)
return 1; // it is, so call output 1
return 2; // it isn't so call output 2";
        var result = pm.Execute(args);
        Assert.IsTrue(result > 0);
    }


    [TestMethod]
    public void Function_FileNameStringVariable()
    {
        Function pm = new Function();
        var newFile = System.IO.Path.Combine(TempPath, "movie h264.mkv");
        System.IO.File.Move(TempFile, newFile);
        var args = new FileFlows.Plugin.NodeParameters(newFile, Logger, false, string.Empty, new LocalFileService(false));
        args.InitFile(newFile);
        args.ScriptExecutor = new ScriptExecutor();
        pm.Code = @"
let newName = Variables.file.Name;

if (newName.indexOf('h264') > 0)
newName = newName.replace('h264', 'h265');
else if (newName.indexOf('hevc') > 0)
newName = newName.replace('hevc', 'h265');
else
newName += ' h265';
if (newName == Variables.file.Name)
return 2;

Variables.NewName = newName;
return 1;";
        var result = pm.Execute(args);
        Assert.AreEqual(1, result);
        Assert.AreEqual("movie h265.mkv", args.Variables["NewName"]);
    }


    [TestMethod]
    public void Function_CropVariable()
    {
        Function pm = new Function();
        var args = new FileFlows.Plugin.NodeParameters(@"D:\videos\unprocessed\movie h264.mkv", Logger, false, string.Empty, MockFileService.Object);
        args.ScriptExecutor = new ScriptExecutor();
        pm.Code = @"
let quality = Variables.VideoCrop ? 17 : 19;
Variables.VideoCodecParameters = `hevc_qsv -preset slow -tune film -global_quality ${quality} -look_ahead 1`;
Variables.VideoCodec = 'h265';
Variables.Extension = 'mkv';
return 1;
        ; ";
        args.Variables["VideoCrop"] = "1920:1000:40:40";
        var result = pm.Execute(args);
        Assert.AreEqual(1, result);
        Assert.AreEqual("hevc_qsv -preset slow -tune film -global_quality 17 -look_ahead 1", args.Variables["VideoCodecParameters"]);
    }

    [TestMethod]
    public void Function_CropVariable_Missing()
    {
        Function pm = new Function();
        var args = new FileFlows.Plugin.NodeParameters(TempFile, Logger, false, string.Empty, MockFileService.Object);
        args.ScriptExecutor = new ScriptExecutor();
        pm.Code = @"
let quality = Variables.VideoCrop ? 17 : 19;
Variables.VideoCodecParameters = `hevc_qsv -preset slow -tune film -global_quality ${quality} -look_ahead 1`;
Variables.VideoCodec = 'h265';
Variables.Extension = 'mkv';
return 1;
        ; ";
        var result = pm.Execute(args);
        Assert.AreEqual(1, result);
        Assert.AreEqual("hevc_qsv -preset slow -tune film -global_quality 19 -look_ahead 1", args.Variables["VideoCodecParameters"]);
    }
    
    

    [TestMethod]
    public void Function_Exec_Callback()
    {
        Function pm = new Function();
        pm.Code = $@"
// Define the callback functions in C#
var executeArgs = new ExecuteArgs();
executeArgs.Command = 'ping';
executeArgs.ArgumentList = [
    '-c',
    '2',
    '8.8.8.8'
];

// Set up the callback output
let standardError = false;
let standardOutput = false;
executeArgs.add_Output((line) => {{
    Logger.ILog(""Standard Output: "" + line);
    standardOutput = true;
}});
executeArgs.add_Error((line) => {{
    Logger.ILog(""Error Output: "" + line);
    standardError = true;
}});

Flow.Execute(executeArgs);

Logger.ILog('standardOutput: ' + standardOutput);
Logger.ILog('standardError: ' + standardError);

return standardOutput || standardError ? 1 : 2;
";
        var result = pm.Execute(Args);
        Assert.AreEqual(1, result);
    }
}