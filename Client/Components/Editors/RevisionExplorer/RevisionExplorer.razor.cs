using FileFlows.Client.Components.Common;
using Humanizer;

namespace FileFlows.Client.Components.Editors;

/// <summary>
/// A component that displays a modal interface for exploring the revisions of a specific object.
/// </summary>
public partial class RevisionExplorer : ModalEditor
{ 
    private Guid ObjectUid;
    private List<RevisionedObject> Revisions = new ();
    public FlowTable<RevisionedObject> Table { get; set; }

    private ModalEditorWrapper? Wrapper;


    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        ReadOnly = true; // we dont have a save button
        var uid = GetModelUid();
        
        ObjectUid = uid;
        Title = Translater.Instant("Labels.Revisions");
        Wrapper?.ShowBlocker();
        var revisionResponse = await HttpHelper.Get<RevisionedObject[]>("/api/revision/" + uid);
        if (revisionResponse.Success == false)
        {
            Close();
            return;
        }

        Revisions = revisionResponse.Data.ToList();
        Wrapper?.HideBlocker();
    }

    /// <summary>
    /// Restores a selected revisioned object to its previous state.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation of restoring the selected revisioned object.
    /// </returns>
    private async Task Restore()
    {
        Wrapper?.ShowBlocker("Labels.Restoring");
        try
        {
            var item = Table.GetSelected().FirstOrDefault();
            var result = await HttpHelper.Put("/api/revision/" + item.Uid + "/restore/" + ObjectUid);
            if (result.Success)
            {
                feService.Notifications.ShowSuccess(Translater.Instant("Labels.RestoredMessage",
                    new { type = item.RevisionType[(item.RevisionType.LastIndexOf('.') + 1)..].Humanize(LetterCasing.Title) }));
                Close();
            }
            else
            {
                feService.Notifications.ShowError(Translater.Instant("Labels.RestoredFailedMessage",
                    new { type = item.RevisionType[(item.RevisionType.LastIndexOf('.') + 1)..].Humanize(LetterCasing.Title) }));
            }
        }
        catch (Exception ex)
        {
            feService.Notifications.ShowError(ex.Message);
        }
        finally
        {
            Wrapper?.HideBlocker();
        }
    }
    
}
