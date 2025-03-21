using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FileFlows.Shared.Helpers;

/// <summary>
/// Helper to debounce an action/method.
/// </summary>
public static class DebounceHelper
{
    private static readonly ConcurrentDictionary<string, Timer> Timers = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    /// <summary>
    /// Debounces an action, ensuring it only executes after the specified delay if not called again.
    /// </summary>
    /// <param name="key">A unique key to track the debounce instance.</param>
    /// <param name="delay">The time span to wait before executing the action.</param>
    /// <param name="action">The action to execute.</param>
    public static void Debounce(string key, TimeSpan delay, Action action)
    {
        if (Timers.TryGetValue(key, out var existingTimer))
        {
            existingTimer.Change(Timeout.Infinite, Timeout.Infinite);
            existingTimer.Dispose();
        }

        var timer = new Timer(_ =>
        {
            action();
            Timers.TryRemove(key, out var _); // Remove completed timer
        }, null, delay, Timeout.InfiniteTimeSpan);

        Timers[key] = timer;
    }

    /// <summary>
    /// Debounces an asynchronous action, ensuring it only executes after the specified delay if not called again.
    /// </summary>
    /// <param name="key">A unique key to track the debounce instance.</param>
    /// <param name="delay">The time span to wait before executing the action.</param>
    /// <param name="asyncAction">The asynchronous action to execute.</param>
    public static void Debounce(string key, TimeSpan delay, Func<Task> asyncAction)
    {
        if (!Locks.TryGetValue(key, out var semaphore))
        {
            semaphore = new SemaphoreSlim(1, 1);
            Locks[key] = semaphore;
        }

        Debounce(key, delay, () =>
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