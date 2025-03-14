using System.Text;
using System.Text.Json;
using FileFlows.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page the shows the system's DockerMods
/// </summary>
public partial class DockerMods : ListPage<Guid, DockerMod>
{
    /// <summary>
    /// The API URL
    /// </summary>
    public override string ApiUrl => "/api/dockermod";

    /// <summary>
    /// Gets or sets the DockerMod Browser isntance
    /// </summary>
    private RepositoryBrowser Browser { get; set; }

    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] protected IJSRuntime jsRuntime { get; set; }

    /// <summary>
    /// Translated strings
    /// </summary>
    private string lblTitle, lblUpdateAvailable, lblRevision;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblTitle = Translater.Instant("Pages.DockerMod.Plural");
        lblUpdateAvailable = Translater.Instant("Pages.DockerMod.Labels.UpdateAvailable");
        lblRevision = Translater.Instant("Pages.DockerMod.Labels.Revision");
    }


    Task Add()
        => OpenEditor(new ()
        {
            Code = "#!/bin/bash\n\n",
            Enabled = true
        });
    public override Task<bool> Edit(DockerMod item)
        => OpenEditor(item);

    private Task DoubleClick(DockerMod item)
        => OpenEditor(item);

    
    async Task OpenBrowser()
    {
        bool result = await Browser.Open();
        if (result)
            await Refresh();
    }


    async Task Update()
    {
        var items = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new System.Guid[] { };
        if (items?.Any() != true)
            return;
        await Update(items);
    }
    
    async Task Update(params Guid[] items)
    {
        Blocker.Show("Pages.DockerMod.Messages.Updating");
        this.StateHasChanged();
        Data.Clear();
        try
        {
            await HttpHelper.Post($"/api/repository/DockerMod/update", new ReferenceModel<Guid> { Uids = items });
            await Refresh();
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Exports a DockerHub command
    /// </summary>
    private async Task Export()
    {
        var item = Table?.GetSelected()?.FirstOrDefault();
        if (item == null)
            return;
        string url = $"{ApiUrl}/export/{item.Uid}";
#if (DEBUG)
        url = "http://localhost:6868" + url;
#endif
        
        var result = await HttpHelper.Get<string>(url);
        if (result.Success == false)
        {
            Toast.ShowError(Translater.Instant("Pages.DockerMod.Messages.FailedToExport"));
            return;
        }

        await jsRuntime.InvokeVoidAsync("ff.saveTextAsFile", item.Name  + ".sh", result.Body);
    }

    /// <summary>
    /// Moves the items down
    /// </summary>
    /// <returns>a task to await</returns>
    Task MoveUp() => Move(true);

    /// <summary>
    /// Moves the items down
    /// </summary>
    /// <returns>a task to await</returns>
    Task MoveDown() => Move(false);

    /// <summary>
    /// Moves the items up or down
    /// </summary>
    /// <param name="up">if moving up</param>
    async Task Move(bool up)
    {
        var uids = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids.Length == 0)
            return; // nothing to move

        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var result = await HttpHelper.Post($"{ApiUrl}/move?up={up}", new ReferenceModel<Guid> { Uids = uids });
            if (result.Success == false)
            {
                if(Translater.NeedsTranslating(result.Body))
                    Toast.ShowError( Translater.Instant(result.Body));
                else
                    Toast.ShowError( Translater.Instant("Pages.DockerMods.Messages.MoveFailed"));
                return;
            }

            await Refresh();
            var selected = Data.Where(x => uids.Contains(x.Uid)).ToList();
            Table.SetSelected(selected);
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }
}