namespace FileFlows.AvaloniaUi;

/// <summary>
/// A message handler
/// </summary>
public interface IMessageHandler
{
    /// <summary>
    /// Show a confirmation prompt
    /// </summary>
    /// <param name="title">the title of the confirmation prompt</param>
    /// <param name="message">the message of the confirmation prompt</param>
    /// <returns>the confirmation result</returns>
    Task<bool> Confirm(string title, string message);

    /// <summary>
    /// Show a message 
    /// </summary>
    /// <param name="title"></param>
    /// <param name="message"></param>
    /// <returns>the task to await</returns>
    Task Message(string title, string message);
}