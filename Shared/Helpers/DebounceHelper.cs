using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FileFlows.Shared.Helpers;

/// <summary>
/// Helper to debounce an action/method with an optional maximum delay guarantee.
/// </summary>
public static class DebounceHelper
{
    private static readonly ConcurrentDictionary<string, (Timer Timer, DateTime FirstQueuedTime)> Timers = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    /// <summary>
    /// Debounces an action, ensuring it only executes after the specified delay if not called again.
    /// </summary>
    /// <param name="key">A unique key to track the debounce instance.</param>
    /// <param name="delay">The time span to wait before executing the action.</param>
    /// <param name="action">The action to execute.</param>
    public static void Debounce(string key, TimeSpan delay, Action action)
        => Debounce(key, delay, null, action);

    /// <summary>
    /// Debounces an asynchronous action, ensuring it only executes after the specified delay if not called again.
    /// </summary>
    /// <param name="key">A unique key to track the debounce instance.</param>
    /// <param name="delay">The time span to wait before executing the action.</param>
    /// <param name="asyncAction">The asynchronous action to execute.</param>
    public static void Debounce(string key, TimeSpan delay, Func<Task> asyncAction)
        => Debounce(key, delay, null, asyncAction);

    /// <summary>
    /// Debounces an action, ensuring it only executes after the specified delay if not called again.
    /// Optionally, guarantees execution at most after maxDelay.
    /// </summary>
    /// <param name="key">A unique key to track the debounce instance.</param>
    /// <param name="delay">The time span to wait before executing the action.</param>
    /// <param name="maxDelay">The maximum time span before execution is forced (optional).</param>
    /// <param name="action">The action to execute.</param>
    public static void Debounce(string key, TimeSpan delay, TimeSpan? maxDelay, Action action)
    {
        if (Timers.TryGetValue(key, out var existingEntry))
        {
            existingEntry.Timer.Change(Timeout.Infinite, Timeout.Infinite);
            existingEntry.Timer.Dispose();

            if (maxDelay.HasValue && (DateTime.UtcNow - existingEntry.FirstQueuedTime) >= maxDelay.Value)
            {
                Timers.TryRemove(key, out _);
                action();
                return;
            }
        }

        DateTime firstQueuedTime = existingEntry.FirstQueuedTime == default
            ? DateTime.UtcNow
            : existingEntry.FirstQueuedTime;

        var timer = new Timer(_ =>
        {
            action();
            Timers.TryRemove(key, out var _);
        }, null, delay, Timeout.InfiniteTimeSpan);

        Timers[key] = (timer, firstQueuedTime);
    }

    /// <summary>
    /// Debounces an asynchronous action, ensuring it only executes after the specified delay if not called again.
    /// Optionally, guarantees execution at most after maxDelay.
    /// </summary>
    /// <param name="key">A unique key to track the debounce instance.</param>
    /// <param name="delay">The time span to wait before executing the action.</param>
    /// <param name="maxDelay">The maximum time span before execution is forced (optional).</param>
    /// <param name="asyncAction">The asynchronous action to execute.</param>
    public static void Debounce(string key, TimeSpan delay, TimeSpan? maxDelay, Func<Task> asyncAction)
    {
        if (!Locks.TryGetValue(key, out var semaphore))
        {
            semaphore = new SemaphoreSlim(1, 1);
            Locks[key] = semaphore;
        }

        Debounce(key, delay, maxDelay, () =>
        {
            if (semaphore.Wait(0))
            {
                try
                {
                    asyncAction().Wait();
                }
                finally
                {
                    semaphore.Release();
                }
            }
        });
    }
}
