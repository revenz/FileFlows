using System.Threading;

namespace FileFlowsTests.Tests;

/// <summary>
/// Unit tests for <see cref="ProcessHelper"/> class to test shell command execution.
/// </summary>
[TestClass]
public class ProcessHelperTests : TestBase
{
    /// <summary>
    /// Tests a successful execution of a shell command.
    /// </summary>
    [TestMethod]
    public async Task Good()
    {
        // Arrange
        ProcessHelper helper = new(Logger, CancellationToken.None, false);

        var startTime = DateTime.Now; // Capture start time for duration measurement

        // Act
        var result = await helper.ExecuteShellCommand(new()
        {
            Command = "echo", // `echo` command will execute successfully
            ArgumentList = new[] { "Hello", "World" },
            Timeout = 10
        });

        var duration = DateTime.Now - startTime; // Calculate duration
        Logger.ILog("Duration: " + duration);
        
        // Assert
        Assert.IsTrue(result.Completed);
        Assert.AreEqual("Hello World", result.Output.Trim());
        Assert.AreEqual(0, result.ExitCode);

        // Assert that the execution time is reasonable (< 2 seconds)
        Assert.IsTrue(duration.TotalSeconds < 2, $"Test took {duration.TotalSeconds} seconds, expected < 2 seconds.");
    }

    /// <summary>
    /// Tests a command that exceeds the timeout, causing it to be killed.
    /// </summary>
    [TestMethod]
    public async Task Timeout()
    {
        // Arrange
        ProcessHelper helper = new(Logger, CancellationToken.None, false);

        var startTime = DateTime.Now; // Capture start time for duration measurement

        // Act
        var result = await helper.ExecuteShellCommand(new()
        {
            Command = "sleep", // `sleep` command will run indefinitely until timeout
            ArgumentList = new[] { "60" }, // Sleep for 60 seconds
            Timeout = 5, // Timeout after 5 seconds
        });

        var duration = DateTime.Now - startTime; // Calculate duration
        Logger.ILog("Duration: " + duration);
        
        // Assert
        Assert.IsFalse(result.Completed);
        Assert.IsNull(result.ExitCode);

        // Assert that the execution time is within a sensible range (~ 5 seconds)
        Assert.IsTrue(duration.TotalSeconds >= 5 && duration.TotalSeconds < 6,
            $"Test took {duration.TotalSeconds} seconds, expected between 5 and 6 seconds.");
    }

    /// <summary>
    /// Tests a long-running process and ensures it times out as expected.
    /// </summary>
    [TestMethod]
    public async Task Timeout_LongProcess()
    {
        // Arrange
        ProcessHelper helper = new(Logger, CancellationToken.None, false);

        var startTime = DateTime.Now; // Capture start time for duration measurement

        // Act
        var result = await helper.ExecuteShellCommand(new()
        {
            Command = "sleep", // `sleep` command to simulate long process
            ArgumentList = new[] { "60" },
            Timeout = 10, // Set timeout to 10 seconds
        });

        var duration = DateTime.Now - startTime; // Calculate duration
        Logger.ILog("Duration: " + duration);
        
        // Assert
        Assert.IsFalse(result.Completed);

        // Assert that the execution time is within a sensible range (~ 10 seconds)
        Assert.IsTrue(duration.TotalSeconds >= 10 && duration.TotalSeconds < 11,
            $"Test took {duration.TotalSeconds} seconds, expected between 10 and 11 seconds.");
    }
}
