namespace FileFlowsTests.Tests;

/// <summary>
/// Test base file
/// </summary>
public class TestBase
{
    /// <summary>
    /// The test context instance
    /// </summary>
    private TestContext testContextInstance;

    /// <summary>
    /// Gets or sets the test context
    /// </summary>
    public TestContext TestContext
    {
        get => testContextInstance;
        set => testContextInstance = value;
    }

    /// <summary>
    /// When the test starts
    /// </summary>
    [TestInitialize]
    public void TestStarted()
    {
        Logger.Writer = (message) => TestContext.WriteLine(message);
        var tempPath = Environment.GetEnvironmentVariable("FF_TEMP_PATH");
        if (string.IsNullOrWhiteSpace(tempPath))
            tempPath = System.IO.Path.GetTempPath();
        TempPath = System.IO.Path.Combine(tempPath, "tests", Guid.NewGuid().ToString());
        System.IO.Directory.CreateDirectory(TempPath);

        TestStarting();
    }

    /// <summary>
    /// Cleans up after the tests
    /// </summary>
    [TestCleanup]
    public void TestCleanUp()
    {
        if (System.IO.Directory.Exists(TempPath))
            System.IO.Directory.Delete(TempPath, true);
    }

    /// <summary>
    /// Called when a test starts
    /// </summary>
    protected virtual void TestStarting()
    {

    }

    /// <summary>
    /// The test logger
    /// </summary>
    public readonly TestLogger Logger = new ();
    
    /// <summary>
    /// The resources test file directory
    /// </summary>
    protected static readonly string ResourcesTestFilesDir = "Resources/TestFiles";

    /// <summary>
    /// A path in the temp directory created for the test
    /// </summary>
    public string TempPath { get; private set; } = null!;

    /// <summary>
    /// Gets a temporary file name
    /// </summary>
    /// <returns>the temp filename</returns>
    public string GetTempFileName()
        => System.IO.Path.Combine(TempPath, Guid.NewGuid().ToString());

    /// <summary>
    /// Creates a temporary file name
    /// </summary>
    /// <returns>the temp filename</returns>
    public string CreateTempFile()
    {
        var filename = System.IO.Path.Combine(TempPath, Guid.NewGuid() + ".txt");
        System.IO.File.WriteAllText(filename, "this is a test files");
        return filename;
    }
}