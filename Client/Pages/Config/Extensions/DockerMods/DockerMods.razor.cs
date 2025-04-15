using System.Text;
using System.Text.Json;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Editors;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RepositoryBrowser = FileFlows.Client.Components.Editors.RepositoryBrowser;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page the shows the system's DockerMods
/// </summary>
public partial class DockerMods : ListPage<Guid, DockerMod>, IDisposable
{
    /// <summary>
    /// The API URL
    /// </summary>
    public override string ApiUrl => "/api/dockermod";
    
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }

    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] protected IJSRuntime jsRuntime { get; set; }

    private bool _Moving = false;

    /// <summary>
    /// Translated strings
    /// </summary>
    private string lblUpdateAvailable, lblRevision;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Layout.SetInfo(Translater.Instant("Pages.DockerMod.Plural"), "fab fa-docker");
        Profile = feService.Profile.Profile;
        base.OnInitialized(false);
        lblUpdateAvailable = Translater.Instant("Pages.DockerMod.Labels.UpdateAvailable");
        lblRevision = Translater.Instant("Pages.DockerMod.Labels.Revision");
        feService.DockerMod.DockerModsUpdated += DockerModOnDockerModsUpdated;
        Data = feService.DockerMod.DockerMods.OrderBy(x => x.Order).ThenBy(x => x.Name).ToList();
    }

    /// <summary>
    /// Called when the DockerMods are updated
    /// </summary>
    /// <param name="obj">the updated items</param>
    private void DockerModOnDockerModsUpdated(List<DockerMod> obj)
    {
        if (_Moving)
            return; // we manually refresh so the selected item isnt lost
        
        Data = obj.OrderBy(x => x.Order).ThenBy(x => x.Name).ToList();
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Table.SetData(Data);
            StateHasChanged();
        }

        base.OnAfterRender(firstRender);
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
        await ModalService.ShowModal<RepositoryBrowser>(new RepositoryOptions()
        {
            Type = RepositoryType.DockerMod
        });
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
            feService.Notifications.ShowError(Translater.Instant("Pages.DockerMod.Messages.FailedToExport"));
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
            _Moving = true;
            var result = await HttpHelper.Post($"{ApiUrl}/move?up={up}", new ReferenceModel<Guid> { Uids = uids });
            if (result.Success == false)
            {
                if(Translater.NeedsTranslating(result.Body))
                    feService.Notifications.ShowError( Translater.Instant(result.Body));
                else
                    feService.Notifications.ShowError( Translater.Instant("Pages.DockerMods.Messages.MoveFailed"));
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
            _Moving = false;
        }
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.DockerMod.DockerModsUpdated -= DockerModOnDockerModsUpdated;
    }
}