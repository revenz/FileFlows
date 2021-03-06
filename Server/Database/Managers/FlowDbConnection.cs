using FileFlows.Plugin;

namespace FileFlows.Server.Database.Managers;

/// <summary>
/// A wrapped database connection that limits the number of open connections
/// </summary>
public class FlowDbConnection:IDisposable
{
    private const int MAX_CONNECTIONS = 30;
    private static SemaphoreSlim semaphore = new SemaphoreSlim(MAX_CONNECTIONS, MAX_CONNECTIONS);

    /// <summary>
    /// Gets the database instance
    /// </summary>
    public NPoco.Database Db { get; private set; }

    private FlowDbConnection()
    {
    }

    /// <summary>
    /// Gets the count of open database connections
    /// </summary>
    public static int GetOpenConnections => MAX_CONNECTIONS - semaphore.CurrentCount;

    /// <summary>
    /// Gets a database connection
    /// </summary>
    /// <param name="instanceCreator">action that creates an instance of the database</param>
    /// <returns>the database connection</returns>
    public static async Task<FlowDbConnection> Get(Func<NPoco.Database> instanceCreator)
    {
        DateTime dt = DateTime.Now;
        await semaphore.WaitAsync();
        var timeWaited = DateTime.Now.Subtract(dt);
        if (timeWaited.TotalMilliseconds > 10)
        {
            await FlowDatabase.Logger.Log(LogType.Info, "Time waited for connection: " + timeWaited);
        }
        var db = new FlowDbConnection();
        db.Db = instanceCreator();
        return db;
    }

    public void Dispose()
    {
        if (Db != null)
        {
            Db.Dispose();
            Db = null;
        }

        semaphore.Release();
    }
}