namespace FileFlowsTests.Tests;

/// <summary>
/// Contains unit tests for the <see cref="LanguageHelper"/> class.
/// </summary>
[TestClass]
public class LanguageHelperTests: TestBase
{
    /// <summary>
    /// The language helper instance to use
    /// </summary>
    private LanguageHelper _languageHelper;

    private NodeParameters args;

    /// <inheritdoc />
    protected override void TestStarting()
    {
        _languageHelper = new ();
        args = new(null, Logger, false, null, null);
    }

    /// <summary>
    /// Test method for alias matches
    /// </summary>
    [TestMethod]
    public void Matches_Aliases()
    {
        Assert.IsTrue(LanguageHelper.Matches(args, "chi", "chi"));
        Assert.IsTrue(LanguageHelper.Matches(args, "zh", "zh"));
        Assert.IsTrue(LanguageHelper.Matches(args, "zho", "Chinese"));
        
        Assert.IsTrue(LanguageHelper.Matches(args, "eng", "eng"));
        Assert.IsTrue(LanguageHelper.Matches(args, "eng", "en"));
        Assert.IsTrue(LanguageHelper.Matches(args, "eng", "English"));
        
        Assert.IsTrue(LanguageHelper.Matches(args, "deu", "deu"));
        Assert.IsTrue(LanguageHelper.Matches(args, "deu", "de"));
        Assert.IsTrue(LanguageHelper.Matches(args, "deu", "German"));
        Assert.IsTrue(LanguageHelper.Matches(args, "deu", "ger"));
        args.Variables["OriginalLanguage"] = "deu";
        Assert.IsTrue(LanguageHelper.Matches(args, "orig", "deu"));
        Assert.IsTrue(LanguageHelper.Matches(args, "orig", "de"));
        Assert.IsTrue(LanguageHelper.Matches(args, "orig", "German"));
        Assert.IsTrue(LanguageHelper.Matches(args, "orig", "ger"));
        Assert.IsTrue(LanguageHelper.Matches(args, "/orig/", "ger"));
    }
    

    /// <summary>
    /// Test method for not alias matches
    /// </summary>
    [TestMethod]
    public void Matches_Aliases_Not()
    {
        Assert.IsFalse(LanguageHelper.Matches(args, "fre", "eng"));
        Assert.IsFalse(LanguageHelper.Matches(args, "fre", "en"));
        Assert.IsFalse(LanguageHelper.Matches(args, "fre", "English"));
        
        Assert.IsFalse(LanguageHelper.Matches(args, "jpn", "deu"));
        Assert.IsFalse(LanguageHelper.Matches(args, "jpn", "de"));
        Assert.IsFalse(LanguageHelper.Matches(args, "jpn", "German"));
        Assert.IsFalse(LanguageHelper.Matches(args, "jpn", "ger"));
        args.Variables["OriginalLanguage"] = "deu";
        Assert.IsFalse(LanguageHelper.Matches(args, "orig", "jpn"));
        Assert.IsFalse(LanguageHelper.Matches(args, "orig", "jp"));
        Assert.IsFalse(LanguageHelper.Matches(args, "orig", "Japanese"));
        Assert.IsFalse(LanguageHelper.Matches(args, "/orig/", "jpn"));
    }

    /// <summary>
    /// Test method for exact match.
    /// </summary>
    [TestMethod]
    public void Matches_ExactMatch_ReturnsTrue()
    {
        Assert.IsTrue(LanguageHelper.Matches(args, "eng", "eng"));
        Assert.IsFalse(LanguageHelper.Matches(args, "eng", "fre"));
        Assert.IsFalse(LanguageHelper.Matches(args, "!eng", "eng"));
        Assert.IsTrue(LanguageHelper.Matches(args, "!eng", "fre"));
    }
    

    /// <summary>
    /// Test method a regex single match.
    /// </summary>
    [TestMethod]
    public void Matches_RegexMatch_ReturnsTrue()
    {
        Assert.IsTrue(LanguageHelper.Matches(args, "/eng/", "eng"));
        Assert.IsFalse(LanguageHelper.Matches(args, "/eng/", "fre"));
    }

    /// <summary>
    /// Test method a regex multiple match.
    /// </summary>
    [TestMethod]
    public void Matches_RegexMatchMultiple_ReturnsTrue()
    {
        Assert.IsTrue(LanguageHelper.Matches(args, "/eng|deu/", "eng"));
        Assert.IsFalse(LanguageHelper.Matches(args, "/eng|deu/", "fre"));
    }

    /// <summary>
    /// Test method for regex match with a variable
    /// </summary>
    [TestMethod]
    public void Matches_RegexMatchVariable_ReturnsTrue()
    {
        args.Variables["OriginalLanguage"] = "deu";
        Assert.IsTrue(LanguageHelper.Matches(args, "/orig|eng/", "eng"));
        Assert.IsFalse(LanguageHelper.Matches(args, "/orig|eng/", "fre"));
        Assert.IsTrue(LanguageHelper.Matches(args, "/orig|eng/", "deu"));
        Assert.IsFalse(LanguageHelper.Matches(args, "/orig|eng/", "fre"));
        Assert.IsTrue(LanguageHelper.Matches(args, "!/fre|eng/", "deu"));
        Assert.IsFalse(LanguageHelper.Matches(args, "!/orig|eng/", "eng"));
    }

    /// <summary>
    /// Test method for German languages
    /// </summary>
    [TestMethod]
    public void Matches_German()
    {
        args.Variables["OriginalLanguage"] = "deu";
        Assert.IsTrue(LanguageHelper.Matches(args, "German", "deu"));
        Assert.IsTrue(LanguageHelper.Matches(args, "German", "de"));
        Assert.IsTrue(LanguageHelper.Matches(args, "German", "ger"));
        Assert.IsTrue(LanguageHelper.Matches(args, "German", "Deutsch"));
        Assert.IsTrue(LanguageHelper.Matches(args, "German", "allemand"));
        
        Assert.IsTrue(LanguageHelper.Matches(args, "deu", "deu"));
        Assert.IsTrue(LanguageHelper.Matches(args, "deu", "de"));
        Assert.IsTrue(LanguageHelper.Matches(args, "deu", "ger"));
        Assert.IsTrue(LanguageHelper.Matches(args, "deu", "Deutsch"));
        Assert.IsTrue(LanguageHelper.Matches(args, "deu", "allemand"));
        
        Assert.IsTrue(LanguageHelper.Matches(args, "de", "deu"));
        Assert.IsTrue(LanguageHelper.Matches(args, "de", "de"));
        Assert.IsTrue(LanguageHelper.Matches(args, "de", "ger"));
        Assert.IsTrue(LanguageHelper.Matches(args, "de", "Deutsch"));
        Assert.IsTrue(LanguageHelper.Matches(args, "de", "allemand"));
        
    }
}