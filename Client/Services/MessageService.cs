using FileFlows.Client.Components.Dialogs;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Services;

/// <summary>
/// Implementation of the message service.
/// </summary>
public class MessageService
{
    private readonly IModalService ModalService;

    /// <summary>
    /// Constructs a new instance of the message service
    /// </summary>
    /// <param name="modalService">the modal service</param>
    public MessageService(IModalService modalService)
    {
        this.ModalService = modalService;
    }


    /// <summary>
    /// Shows a confirm message
    /// </summary>
    /// <param name="title">the title of the confirm message</param>
    /// <param name="message">the message of the confirm message</param>
    /// <param name="defaultValue">the default value to have highlighted, true for confirm, false for reject</param>
    /// <returns>the task to await for the confirm result</returns>
    public async Task<bool> Confirm(string title, string message, bool defaultValue = true)
    {
        var result = await ModalService.ShowModal<Confirm, bool>(new ConfirmOptions()
        {
            Title = title,
            Message = message,
            DefaultValue = defaultValue,
        });
        
        return result is { IsFailed: false, Value: true };
    }
    
    /// <summary>
    /// Shows a confirmation message
    /// </summary>
    /// <param name="title">the title of the confirmation message</param>
    /// <param name="message">the message of the confirmation message</param>
    /// <param name="switchMessage">message to show with an extra switch</param>
    /// <param name="switchState">the switch state</param>
    /// <param name="requireSwitch">if the switch is required to be checked for the YES button to become enabled</param>
    /// <returns>the task to await for the confirm result</returns>
    public async Task<(bool Confirmed, bool SwitchState)> Confirm(string title, string message, string switchMessage, bool switchState = false, bool requireSwitch = false)
    {
        var result = await ModalService.ShowModal<Confirm, (bool, bool)>(new ConfirmOptions()
        {
            Title = title,
            Message = message,
            SwitchMessage = switchMessage,
            ShowSwitch = true,
            RequireSwitch = requireSwitch,
            SwitchState = switchState
        });
        if (result.IsFailed)
            return (false, false);
        return result.Value;
    }

    /// <summary>
    /// Shows a message
    /// </summary>
    /// <param name="title">the title of the message</param>
    /// <param name="message">the message of the message</param>
    /// <returns>the task to await for the message box to close</returns>
    public async Task Message(string title, string message)
    {
        await ModalService.ShowModal<MessageBox>(new MessageBoxOptions()
        {
            Title = title,
            Message = message
        });
    }

    /// <summary>
    /// Shows a used by dialog
    /// </summary>
    /// <param name="usedBy">a list of items this is used by</param>
    /// <returns>the task to await for the dialog to close</returns>
    public async Task UsedBy(List<ObjectReference> usedBy)
    {
        await ModalService.ShowModal<UsedByDialog>(new UsedByDialogOptions()
        {
            UsedBy = usedBy
        });
    }
}
