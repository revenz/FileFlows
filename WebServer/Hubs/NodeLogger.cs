using System.Collections.Concurrent;

namespace FileFlows.WebServer.Hubs;

/// <summary>
/// Node Loggers
/// </summary>
public class NodeLogger
{
    /// <summary>
    /// The loggers
    /// </summary>
    private static ConcurrentDictionary<string, FileLogger> Loggers = new ();

    /// <summary>
    /// The semaphore to allow the loggers to be thread safe
    /// </summary>
    private static FairSemaphore _semaphoreSlim = new (1);
        
    /// <summary>
    /// Logs messages to the server
    /// </summary>
    /// <param name="nodeAddress">the UID of the node</param>
    /// <param name="messages">The log messages to log</param>
    public async Task Log(string nodeAddress, string[] messages)
    {
        try
        {
            if (messages == null || messages.Length == 0)
                return;
            
            if (string.IsNullOrEmpty(nodeAddress))
                return;

            await _semaphoreSlim.WaitAsync();
            try
            {
                if (Loggers.TryGetValue(nodeAddress, out var logger) == false)
                {
                    string name = nodeAddress;
                    if (IsValidFileName(name) == false)
                        return;

                    Loggers[nodeAddress] = new(DirectoryHelper.LoggingDirectory, "Node-" + name, false);
                    logger = Loggers[nodeAddress];
                }
                
                await logger.LogRaw(messages);
            }
            catch (Exception)
            {
                // Ignored
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
        catch(Exception)
        {
            // Ignore    
        }
    }

    /// <summary>
    /// Replaces the complete log with one from the node
    /// </summary>
    /// <param name="nodeAddress">the UID of the node</param>
    /// <param name="log">The complete log from the node</param>
    public async Task Sync(string nodeAddress, string log)
    {
        try
        {
            if (string.IsNullOrEmpty(nodeAddress) || string.IsNullOrEmpty(log))
                return;

            await _semaphoreSlim.WaitAsync();
            try
            {
                if (Loggers.TryGetValue(nodeAddress, out var logger) == false)
                {
                    string name = nodeAddress;
                    if (IsValidFileName(name) == false)
                        return;

                    Loggers[nodeAddress] = new(DirectoryHelper.LoggingDirectory, "Node-" + name, false);
                    logger = Loggers[nodeAddress];
                }
                
                await logger.SetLogContent(log);
            }
            catch (Exception)
            {
                // Ignored
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
        catch(Exception)
        {
            // Ignore    
        }
        
    }
    
    /// <summary>
    /// Checks if a file name is valid.
    /// </summary>
    /// <param name="fileName">The file name to validate.</param>
    /// <returns>True if the file name is valid; otherwise, false.</returns>
    /// <remarks>
    /// Valid file names consist only of alphanumeric characters, hyphens, and underscores.
    /// They do not contain sequences like '..' or '/' to prevent directory traversal.
    /// </remarks>
    public bool IsValidFileName(string fileName)
    {
        // Match only alphanumeric characters, hyphen, and underscore
        // Ensure it does not contain '..' or '/'
        return Regex.IsMatch(fileName, @"^[a-zA-Z0-9-_]+$") && fileName.Contains("..") == false && fileName.Contains("/") == false;
    }
}