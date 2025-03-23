namespace FileFlows.DataLayer.DatabaseConnectors;

public class DatabaseConnection : IDisposable
{
    public FileFlowsDb Db { get; private set; }
    private bool Disposable;
    public bool IsDisposed { get;private set; }

    public EventHandler? OnDispose;
    
    /// <summary>
    /// Constructs an intance of the Databaase Connection
    /// </summary>
    /// <param name="db">The database</param>
    /// <param name="disposable">if this database is disposable</param>
    public DatabaseConnection(FileFlowsDb db, bool disposable)
    {
        Db = db;
        Disposable = disposable;
    }

    // public static implicit operator NPoco.Database(DatabaseConnection value)
    //     => value.Db;

    public void Dispose()
    {
        IsDisposed = true;
        if(Disposable)
            Db.Dispose();
        OnDispose?.Invoke(this, new EventArgs());
    }
}