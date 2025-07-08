using System.IO;
using FileFlows.FlowRunner.Helpers;
using FileFlows.Plugin.Helpers;
using FileFlows.ServerShared.FileServices;

namespace FileFlowsTests.Tests.RunnerTests.ImageHelperTests;

/// <summary>
/// Tests Creduts
/// </summary>
[TestClass]
public class CreditsTests : TestBase
{
    /// <summary>
    /// Test ifles
    /// </summary>
    string Not01 = Path.Combine(ResourcesTestFilesDir, "Credits", "not01.jpg");
    string Not02 = Path.Combine(ResourcesTestFilesDir, "Credits", "not02.png");
    string Not03 = Path.Combine(ResourcesTestFilesDir, "Credits", "not03.jpg");
    string Not04 = Path.Combine(ResourcesTestFilesDir, "Credits", "not04.jpg");
    string Not05 = Path.Combine(ResourcesTestFilesDir, "Credits", "not05.jpg");
    string Not06 = Path.Combine(ResourcesTestFilesDir, "Credits", "not06.jpg");
    string Not07 = Path.Combine(ResourcesTestFilesDir, "Credits", "not07.jpg");
    string Not08 = Path.Combine(ResourcesTestFilesDir, "Credits", "not08.jpg");
    
    string Brown = Path.Combine(ResourcesTestFilesDir, "Credits", "brown.png");
    string Logos = Path.Combine(ResourcesTestFilesDir, "Credits", "logos.jpg");
    string Pink = Path.Combine(ResourcesTestFilesDir, "Credits", "pink.png");
    string White = Path.Combine(ResourcesTestFilesDir, "Credits", "white.png");
    string White2 = Path.Combine(ResourcesTestFilesDir, "Credits", "white2.png");

    /// <summary>
    /// Test non credits images arent detected as credits
    /// </summary>
    [TestMethod]
    public void NotTests()
    {
        var helper = new ImageHelper(Logger, "convert", "identify");
        List<CreditResult> results = new ();
        bool success = true;
        foreach (var file in new[]
                 {
                     Not01, Not02, Not03, Not04, Not05, 
                     Not06, Not07, Not08
                 })
        {
            Logger.ILog("Testing file is not credits: " + file);
            var result = helper.IsCreditsOrBlackFrame(file);
            results.Add(new CreditResult(file, result));
            bool isCredits  = result !=  CreditsFrameType.Other;
            success &= !isCredits;
        }
        ((ILogger)Logger).Table(results);
        Assert.IsTrue(success);
    }
    

    /// <summary>
    /// Test credit images are detected as credits
    /// </summary>
    [TestMethod]
    public void YesTests()
    {
        var helper = new ImageHelper(Logger, "convert", "identify");
        
        List<CreditResult> results = new ();
        bool success = true;
        foreach (var file in new[]
                 {
                     Brown,
                     Logos,
                     Pink,
                     White,
                     White2
                 })
        {
            Logger.ILog("Testing file is credits: " + file);
            var result = helper.IsCreditsOrBlackFrame(file);
            results.Add(new CreditResult(file, result));
            bool isCredits  = result !=  CreditsFrameType.Other;
            success &= isCredits;
        }
        
        ((ILogger)Logger).Table(results);
        Assert.IsTrue(success);
    }

    record CreditResult(string Name, CreditsFrameType Credit);
}