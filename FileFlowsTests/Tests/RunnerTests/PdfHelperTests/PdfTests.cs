
#if(DEBUG)
using System.IO;
using FileFlows.FlowRunner.Helpers;
using FileFlows.Plugin.Helpers;
using FileFlows.ServerShared.FileServices;

namespace FileFlowsTests.Tests.RunnerTests.ImageHelperTests;

/// <summary>
/// Tests PDF fiels
/// </summary>
[TestClass]
public class PdfTests : TestBase
{
    /// <summary>
    /// The basic.pdf file
    /// </summary>
    string BasicPdf = Path.Combine(ResourcesTestFilesDir, "Pdfs", "basic.pdf");
    
    /// <summary>
    /// Test that a PDF contains some text
    /// </summary>
    [TestMethod]
    public void ContainsText()
    {
        var helper = new PdfHelper(new StringHelper(Logger));
        var result = helper.ContainsText(BasicPdf, "Somatosensory System");
        if(result.Failed(out string error))
            Assert.Fail(error);
        Assert.IsTrue(result.Value);
        
        result = helper.ContainsText(BasicPdf, "throughout");
        if(result.Failed(out error))
            Assert.Fail(error);
        Assert.IsTrue(result.Value);
    }
    
    /// <summary>
    /// Test that a PDF does not contain some text
    /// </summary>
    [TestMethod]
    public void DoesNotContainText()
    {
        var helper = new PdfHelper(new StringHelper(Logger));
        var result = helper.ContainsText(BasicPdf, "Not Found in Text");
        if(result.Failed(out string error))
            Assert.Fail(error);
        Assert.IsFalse(result.Value);
    }
    
    /// <summary>
    /// Test that a PDF contains some text
    /// </summary>
    [TestMethod]
    public void MatchesText()
    {
        var helper = new PdfHelper(new StringHelper(Logger));
        var result = helper.MatchesText(BasicPdf, "Somatosensory System");
        if(result.Failed(out string error))
            Assert.Fail(error);
        Assert.IsTrue(result.Value);
        
        result = helper.MatchesText(BasicPdf, "*through*");
        if(result.Failed(out error))
            Assert.Fail(error);
        Assert.IsTrue(result.Value);
    }
    
    /// <summary>
    /// Test that a PDF does not match some text
    /// </summary>
    [TestMethod]
    public void DoesNotMatchText()
    {
        var helper = new PdfHelper(new StringHelper(Logger));
        var result = helper.MatchesText(BasicPdf, "=This is a sample document");
        if(result.Failed(out string error))
            Assert.Fail(error);
        Assert.IsFalse(result.Value);
    }

    /// <summary>
    /// Test that a PDF does not match some text
    /// </summary>
    [TestMethod]
    public void StartsWithText()
    {
        var helper = new PdfHelper(new StringHelper(Logger));
        var result = helper.MatchesText(BasicPdf, "This is a sample document*");
        if(result.Failed(out string error))
            Assert.Fail(error);
        Assert.IsTrue(result.Value);
    }
}
#endif