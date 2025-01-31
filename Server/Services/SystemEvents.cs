using FileFlows.Shared.Models;

namespace FileFlows.Server;

/// <summary>
/// FileFlows System Events
/// </summary>
public class SystemEvents : ISystemEventsService
{
    /// <summary>
    /// Event that is fired when a server update is available
    /// </summary>
    public event UpdateEvent OnServerUpdateAvailable;
    
    /// <summary>
    /// Event that is fired when the server is updating
    /// </summary>
    public event UpdateEvent OnServerUpdating;
    
    /// <summary>
    /// Triggers the server update event
    /// </summary>
    /// <param name="version">the version of the update available</param>
    public void TriggerServerUpdateAvailable(string version)
    {
        OnServerUpdateAvailable?.Invoke(new () { Version = version, CurrentVersion = Globals.Version.ToString() });
    }

    
    /// <summary>
    /// Triggers the server updating event
    /// </summary>
    /// <param name="version">the version of the update</param>
    public void TriggerServerUpdating(string version)
    {
        OnServerUpdating?.Invoke(new () { Version = version, CurrentVersion = Globals.Version.ToString() });
    }
    
    /// <summary>
    /// Event that is fired when a library file is added to the system
    /// </summary>
    public event LibraryFileEvent OnLibraryFileAdd;
    
    /// <summary>
    /// Event that is fired when a library file starts processing
    /// </summary>
    public event LibraryFileEvent OnLibraryFileProcessingStarted;
    
    /// <summary>
    /// Event that is fired when a library file finishes processing
    /// </summary>
    public event LibraryFileEvent OnLibraryFileProcessed;
    
    /// <summary>
    /// Event that is fired when a library file finishes processing successfully
    /// </summary>
    public event LibraryFileEvent OnLibraryFileProcessedSuceess;
    
    /// <summary>
    /// Event that is fired when a library file finishes processing failed
    /// </summary>
    public event LibraryFileEvent OnLibraryFileProcessedFailed;

    /// <summary>
    /// Triggers the file added event
    /// </summary>
    /// <param name="file">the file</param>
    /// <param name="library">the library the file is from</param>
    public void TriggerFileAdded(LibraryFile file, Library library)
    {
        OnLibraryFileAdd?.Invoke(new () { File = file, Library = library});
    }
    
    /// <summary>
    /// Triggers the library file processing started event event
    /// </summary>
    /// <param name="file">the file</param>
    /// <param name="library">the library the file is from</param>
    public void TriggerLibraryFileProcessingStarted(LibraryFile file, Library library)
    {
        OnLibraryFileProcessingStarted?.Invoke(new () { File = file, Library = library});
    }
    
    /// <summary>
    /// Triggers the library file processed event event
    /// </summary>
    /// <param name="file">the file</param>
    /// <param name="library">the library the file is from</param>
    public void TriggerLibraryFileProcessed(LibraryFile file, Library library)
    {
        OnLibraryFileProcessed?.Invoke(new () { File = file, Library = library});
    }
    
    /// <summary>
    /// Triggers the library file processed successfully event event
    /// </summary>
    /// <param name="file">the file</param>
    /// <param name="library">the library the file is from</param>
    public void TriggerLibraryFileProcessedSuccess(LibraryFile file, Library library)
    {
        OnLibraryFileProcessedSuceess?.Invoke(new () { File = file, Library = library});
    }
    
    /// <summary>
    /// Triggers the library file processed failed event event
    /// </summary>
    /// <param name="file">the file</param>
    /// <param name="library">the library the file is from</param>
    public void TriggerLibraryFileProcessedFailed(LibraryFile file, Library library)
    {
        OnLibraryFileProcessedFailed?.Invoke(new () { File = file, Library = library});
    }
}