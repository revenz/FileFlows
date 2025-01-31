using System.IO;
using FileFlows.ServerShared.FileServices;
using Microsoft.AspNetCore.Routing.Constraints;

namespace FileFlowsTests.Tests;

/// <summary>
/// Tests for NodeParameters
/// </summary>
[TestClass]
public class NodeParamatersTests: TestBase
{
    [TestMethod]
    public void InitFileTests()
    {
        var original = Path.Combine(TempPath, "original.mkv");
        File.WriteAllText(original, "test");
        
        var nodeParameters = new NodeParameters(original, Logger, false, @"C:\media", new LocalFileService(false));

        var file = Path.Combine(TempPath, "somefile.mp4");
        File.WriteAllText(file, "test");
        
        nodeParameters.InitFile(file);
        
        Assert.AreEqual(".mp4", nodeParameters.Variables["ext"]);
        Assert.AreEqual("somefile.mp4", nodeParameters.Variables["file.Name"]);
        Assert.AreEqual("somefile", nodeParameters.Variables["file.NameNoExtension"]);
        
        Assert.AreEqual(".mkv", nodeParameters.Variables["file.Orig.Extension"]);
        Assert.AreEqual("original.mkv", nodeParameters.Variables["file.Orig.FileName"]);
        Assert.AreEqual("original", nodeParameters.Variables["file.Orig.FileNameNoExtension"]);
        Assert.AreEqual("original.mkv", nodeParameters.Variables["file.Orig.Name"]);
        Assert.AreEqual("original", nodeParameters.Variables["file.Orig.NameNoExtension"]);
    }

    [TestMethod]
    public void VariablesTests()
    {
        
        var original = Path.Combine(TempPath, "original.mkv");
        File.WriteAllText(original, "test");
        
        var nodeParameters = new NodeParameters(original, Logger, false, @"C:\media", new LocalFileService(false));

        var file = Path.Combine(TempPath, "somefile.mp4");
        File.WriteAllText(file, "test");
        
        nodeParameters.InitFile(file);
        
        Assert.AreEqual("Path", nodeParameters.ReplaceVariables("Path{missing}", true));
        Assert.AreEqual("Path{missing}", nodeParameters.ReplaceVariables("Path{missing}", false));
        Assert.AreEqual("", nodeParameters.ReplaceVariables("{missing}", true));
        Assert.AreEqual("{missing}", nodeParameters.ReplaceVariables("{missing}", false));
    }
}