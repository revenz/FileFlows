using FileFlows.Common;
using FileFlows.ScriptExecution;
using Microsoft.AspNetCore.SignalR.Client;

namespace FileFlows.NodeClient;

/// <summary>
/// A custom retry policy for automatic reconnection in SignalR.
/// Implements an increasing retry delay pattern, starting at 5 seconds and growing up to 30 seconds.
/// After a successful connection, the delay will restart from 5 seconds.
/// </summary>
/// <param name="logger">the Logger to use</param>
public class RetryPolicyLoop(ILogger logger) : IRetryPolicy
{
    private int _currentRetryIndex = 0;
    private readonly List<TimeSpan> _retryDelays = new List<TimeSpan>
    {
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(20),
        TimeSpan.FromSeconds(30),
        // Add more or modify the pattern as needed.
    };
    
    private int _retryCount = 0;  // Tracks the number of retry attempts

    /// <summary>
    /// Determines the next retry delay based on the number of retries.
    /// The delay pattern increases from 5 seconds to 30 seconds, and continues indefinitely at 30 seconds after the fifth retry.
    /// </summary>
    /// <param name="retryContext">Context that provides details about the current retry attempt.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the delay before the next retry.</returns>
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
        => NextRetryDelay();

    /// <summary>
    /// Determines the next retry delay based on the number of retries.
    /// The delay pattern increases from 5 seconds to 30 seconds, and continues indefinitely at 30 seconds after the fifth retry.
    /// </summary>
    /// <param name="retryContext">Context that provides details about the current retry attempt.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the delay before the next retry.</returns>
    public TimeSpan NextRetryDelay()
    {
        // If we've exhausted the retry delays, use the max delay (30 seconds) and keep retrying
        if (_currentRetryIndex >= _retryDelays.Count)
        {
            _currentRetryIndex = _retryDelays.Count - 1;  // Keep at the max retry delay
            return _retryDelays[_currentRetryIndex];
        }

        // Get the current delay based on the retry index
        TimeSpan delay = _retryDelays[_currentRetryIndex];
        logger.WLog($"Retry attempt {_currentRetryIndex + 1}: Waiting for {delay.TotalSeconds} seconds before retrying.");
        
        // Increment retry index for next attempt
        _currentRetryIndex++;
        return delay;
    }
    public void ResetBackoff()
    {
        // Reset the backoff to the initial state
        _currentRetryIndex = 0;
    }
}
