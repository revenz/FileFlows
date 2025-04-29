using System.Runtime.CompilerServices;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Services.Frontend;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components;

public partial class RevisionExplorer
{
    public static RevisionExplorer Instance { get;private set; }
    private bool Restored = false;
    
    [CascadingParameter] public Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }
    
    TaskCompletionSource<bool> ShowTask;
    private Guid ObjectUid;
    private string Title;
    private bool Visible;
    private string lblClose;
    private List<RevisionedObject> Revisions = new ();
    private bool AwaitingRender = false;
    public FlowTable<RevisionedObject> Table { get; set; }

    public RevisionExplorer()
    {
        Instance = this;
    }

    protected override void OnInitialized()
    {
        lblClose = Translater.Instant("Labels.Close");
    }

    private async Task Restore()
    {
        Blocker.Show("Labels.Restoring");
        try
        {
            var item = Table.GetSelected().FirstOrDefault();
            var result = await HttpHelper.Put("/api/revision/" + item.Uid + "/restore/" + ObjectUid);
            if (result.Success)
            {
                Restored = true;
                feService.Notifications.ShowSuccess(Translater.Instant("Labels.RestoredMessage",
                    new { type = item.RevisionType[(item.RevisionType.LastIndexOf(".", StringComparison.Ordinal) + 1)..].Humanize(LetterCasing.Title) }));
                this.Close();
            }
            else
            {
                feService.Notifications.ShowError(Translater.Instant("Labels.RestoredFailedMessage",
                    new { type = item.RevisionType[(item.RevisionType.LastIndexOf(".", StringComparison.Ordinal) + 1)..].Humanize(LetterCasing.Title) }));
            }
        }
        catch (Exception ex)
        {
            feService.Notifications.ShowError(ex.Message);
        }
        finally
        {
            Blocker.Hide();
        }
    }
    
    private void Close()
    {
        this.Visible = false;
        this.Revisions.Clear();
        this.ShowTask.SetResult(Restored);
    }

    private async Task AwaitRender()
    {
        AwaitingRender = true;
        this.StateHasChanged();
        await Task.Delay(10);
        while (AwaitingRender)
            await Task.Delay(10);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (AwaitingRender)
            AwaitingRender = false;
    }

    public Task<bool> Show(Guid uid, string name)
    {
        this.ObjectUid = uid;
        this.Title = name;
        this.Restored = false;
        this.Blocker.Show();
        Instance.ShowTask = new TaskCompletionSource<bool>();
        _ = ShowActual(uid);
        return Instance.ShowTask.Task;
    }
    
    private async Task ShowActual(Guid uid)
    {
        try
        {  
            var revisionResponse = await HttpHelper.Get<RevisionedObject[]>("/api/revision/" + uid);
            if (revisionResponse.Success == false)
            {
                ShowTask.SetResult(false);
                return;
            }

            Revisions = revisionResponse.Data.ToList();
            this.Visible = true;
            this.StateHasChanged();
            await AwaitRender();
            this.StateHasChanged();
        }
        finally
        {
            Blocker.Hide();
        }
    }
    
}
