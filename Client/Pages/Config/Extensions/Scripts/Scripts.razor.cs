using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Components.Editors;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RepositoryBrowser = FileFlows.Client.Components.Editors.RepositoryBrowser;

namespace FileFlows.Client.Pages;


/// <summary>
/// Page for processing nodes
/// </summary>
public partial class Scripts : ListPage<Guid, Script>, IDisposable
{
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    
    public override string ApiUrl => "/api/script";

    const string FileFlowsServer = "FileFlowsServer";

    private string TableIdentifier => "Scripts-" + this.SelectedType;
    
    private FlowSkyBox<ScriptType> Skybox;
    [Inject] public IJSRuntime jsRuntime { get; set; }
    
    private List<Script> DataFlow = new();
    private List<Script> DataSystem = new();
    private List<Script> DataShared = new();
    private ScriptType SelectedType = ScriptType.Flow;

    private string lblUpdateScripts, lblUpdatingScripts, lblInUse, lblReadOnly, lblUpdateAvailable;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Profile = feService.Profile.Profile;
        Layout.SetInfo(Translater.Instant("Pages.Scripts.Title"), "fas fa-scroll");
        base.OnInitialized(false);
        lblUpdateScripts = Translater.Instant("Pages.Scripts.Buttons.UpdateAllScripts");
        lblUpdatingScripts = Translater.Instant("Pages.Scripts.Labels.UpdatingScripts");
        lblInUse = Translater.Instant("Labels.InUse");
        lblReadOnly = Translater.Instant("Labels.ReadOnly");
        lblUpdateAvailable = Translater.Instant("Pages.Scripts.Labels.UpdateAvailable");
        feService.Script.ScriptsUpdated += OnScriptsUpdated;
        Data = feService.Script.Scripts;
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            UpdateTypeData();
            StateHasChanged();
        }

        base.OnAfterRender(firstRender);
    }

    /// <summary>
    /// Called when script are updated
    /// </summary>
    /// <param name="data">the updated scripts</param>
    private void OnScriptsUpdated(List<Script> data)
    {
        Data = data;
        UpdateTypeData();
        StateHasChanged();
    }


    private async Task Add()
    {
        ScriptLanguage language;
        if (SelectedType == ScriptType.Shared)
            language = ScriptLanguage.JavaScript;
        else
        {
            var result = await ModalService.ShowModal<ScriptLanguagePicker, ScriptLanguage>(new RepositoryOptions());
            
            if (result.IsFailed)
                return;
            language = result.Value;
        }

        var script = new Script()
        {
            Type = SelectedType,
            Language = language,
            Outputs = language is ScriptLanguage.JavaScript || SelectedType != ScriptType.Flow
                ? null
                : [new(1, "Truthy"), new(2, "Falsy")]
        };

        await Edit(script);
    }

    private async Task Export()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item == null)
            return;
        string url = $"/api/script/export/{item.Uid}";
#if (DEBUG)
        url = "http://localhost:6868" + url;
#endif
        var result = await HttpHelper.Get<string>(url);
        if (result.Success == false)
        {
            feService.Notifications.ShowError(Translater.Instant("Pages.Script.Messages.FailedToExport"));
            return;
        }

        var extension = item.Language switch
        {
            ScriptLanguage.Batch => ".bat",
            ScriptLanguage.Shell => ".sh",
            ScriptLanguage.CSharp => ".cs",
            ScriptLanguage.PowerShell => ".ps1",
            _ => ".js"
        };

        await jsRuntime.InvokeVoidAsync("ff.saveTextAsFile", item.Name + extension, result.Body);
    }

    private async Task Import()
    {
        var result = await ModalService.ShowModal<ImportDialog, ImportDialogResult>(new ImportDialogOptions()
        {
            Extensions = ["js"]
        });

        if (result.IsFailed)
            return;
        string js = result.Value.Content;
        if (string.IsNullOrEmpty(js))
            return;

        Blocker.Show();
        try
        {
            var newItem = await HttpHelper.Post<Script>("/api/script/import?filename=" + UrlEncoder.Create().Encode(result.Value.FileName) + "&type=" + SelectedType, js);
            if (newItem != null && newItem.Success)
            {
                feService.Notifications.ShowSuccess(Translater.Instant("Pages.Scripts.Messages.Imported",
                    new { name = newItem.Data.Name }));
            }
            else
            {
                feService.Notifications.ShowError(newItem.Body?.EmptyAsNull() ?? "Invalid script");
            }
        }
        finally
        {
            Blocker.Hide();
        }
    }


    private async Task Duplicate()
    {
        Blocker.Show();
        try
        {
            var item = Table.GetSelected()?.FirstOrDefault();
            if (item == null)
                return;
            string url = $"/api/script/duplicate/{item.Uid}?type={SelectedType}";
#if (DEBUG)
            url = "http://localhost:6868" + url;
#endif
            var newItem = await HttpHelper.Get<Script>(url);
            if (newItem != null && newItem.Success)
            {
                feService.Notifications.ShowSuccess(Translater.Instant("Pages.Script.Messages.Duplicated",
                    new { name = newItem.Data.Name }));
            }
            else
            {
                feService.Notifications.ShowError(newItem.Body?.EmptyAsNull() ?? "Failed to duplicate");
            }
        }
        finally
        {
            Blocker.Hide();
        }
    }

    protected override string DeleteUrl => $"{ApiUrl}?type={SelectedType}";

    public override async Task Delete()
    {
        var used = Table.GetSelected()?.Any(x => x.UsedBy?.Any() == true) == true;
        if (used)
        {
            feService.Notifications.ShowError("Pages.Scripts.Messages.DeleteUsed");
            return;
        }

        await base.Delete();
    }


    private async Task UsedBy()
    {
        var item = Table.GetSelected()?.FirstOrDefault();
        if (item?.UsedBy?.Any() != true)
            return;
        await UsedByDialog.Show(item.UsedBy);
    }
    
    /// <summary>
    /// Opens the used by dialog
    /// </summary>
    /// <param name="item">the item to open used by for</param>
    /// <returns>a task to await</returns>
    private Task OpenUsedBy(Script item)
        => UsedByDialog.Show(item.UsedBy);
    
    public override Task PostLoad()
    {
        UpdateTypeData();
        return Task.CompletedTask;
    }
    
    private void UpdateTypeData()
    {
        this.DataFlow = this.Data.Where(x => x.Type == ScriptType.Flow).OrderBy(x => x.Name.ToLowerInvariant()).ToList();
        this.DataSystem = this.Data.Where(x => x.Type == ScriptType.System).OrderBy(x => x.Name.ToLowerInvariant()).ToList();
        this.DataShared = this.Data.Where(x => x.Type == ScriptType.Shared).OrderBy(x => x.Name.ToLowerInvariant()).ToList();
        foreach (var script in this.Data)
        {
            if (script.Code?.StartsWith("// path: ") == true)
                script.Code = Regex.Replace(script.Code, @"^\/\/ path:(.*?)$", string.Empty, RegexOptions.Multiline).Trim();
        }

        if (Skybox == null)
            return;
        this.Skybox.SetItems(new List<FlowSkyBoxItem<ScriptType>>()
        {
            new ()
            {
                Name = "Flow Scripts",
                Icon = "fas fa-sitemap",
                Count = this.DataFlow.Count,
                Value = ScriptType.Flow
            },
            Profile.LicensedFor(LicenseFlags.Tasks) ? new ()
            {
                Name = "System Scripts",
                Icon = "fas fa-laptop-code",
                Count = this.DataSystem.Count,
                Value = ScriptType.System
            } : null,
            new ()
            {
                Name = "Shared Scripts",
                Icon = "fas fa-handshake",
                Count = this.DataShared.Count,
                Value = ScriptType.Shared
            }
        }, this.SelectedType);
    }

    async Task Browser()
    {
        await ModalService.ShowModal<RepositoryBrowser>(new RepositoryOptions()
        {
            Type = SelectedType == ScriptType.System ? RepositoryType.ScriptSystem : RepositoryType.ScriptFlow
        });
    }
    
    /// <summary>
    /// Editor for a Script
    /// </summary>
    /// <param name="item">the script to edit</param>
    /// <returns>the result of the edit</returns>
    public override async Task<bool> Edit(Script item)
    {
        var options = new ModalEditorOptions();
        if(item.Uid == Guid.Empty)
        {
            options.Model = item;
        }
        else
        {
            options.Uid = item.Uid;
        }
        
        await ModalService.ShowModal<ScriptEditor>(options);
        return false;
    }

    async Task Update()
    {
        var uids = Table.GetSelected()?.Where(x => string.IsNullOrEmpty(x.Path) == false)?.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids?.Any() != true)
        {
            feService.Notifications.ShowWarning("Pages.Scripts.Messages.NoRepositoryScriptsToUpdate");
            return;
        }

        Blocker.Show("Pages.Scripts.Labels.UpdatingScripts");
        this.StateHasChanged();
        Data.Clear();
        try
        {
            await HttpHelper.Post($"/api/repository/update-specific-scripts", new ReferenceModel<Guid> { Uids = uids });
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    
    private void SetSelected(FlowSkyBoxItem<ScriptType> item)
    {
        SelectedType = item.Value;
        // need to tell table to update so the "Default" column is shown correctly
        Table.TriggerStateHasChanged();
        this.StateHasChanged();
    }

    private async Task UpdateScripts()
    {
        this.Blocker.Show(lblUpdatingScripts);
        try
        {
            await HttpHelper.Post("/api/repository/update-scripts");
            await Task.Delay(1000); // give it time for broadcast to finish
        }
        finally
        {
            this.Blocker.Hide();
        }
    }

    private string GetIcon(Script item)
    {
        string url = "";
#if (DEBUG)
        url = "http://localhost:6868" + url;
#endif
        string nameLower = item.Name.ToLowerInvariant();
        if (nameLower.StartsWith("video"))
            return "/icons/video.svg";
        if (nameLower.StartsWith("fileflows"))
            return "/favicon.svg";
        if (nameLower.StartsWith("image"))
            return "/icons/image.svg";
        if (nameLower.StartsWith("folder"))
            return "/icons/filetypes/folder.svg";
        if (nameLower.StartsWith("file"))
            return url + "/icon/filetype/file.svg";
        if (nameLower.StartsWith("7zip"))
            return url + "/icon/filetype/7z.svg";
        if (nameLower.StartsWith("language"))
            return "fas fa-comments";
        var icons = new[]
        {
            "apple", "apprise", "audio", "basic", "comic", "database", "docker", "emby", "folder", "gotify", "gz",
            "image", "intel", "linux", "nvidia", "plex", "pushbullet", "pushover", "radarr", "sabnzbd", "sonarr", "video", "windows"
        };
        foreach (var icon in icons)
        {
            if (nameLower.StartsWith(icon))
                return $"/icons/{icon}.svg";
        }
        
        if(item.Language is ScriptLanguage.Batch)
            return $"/icons/dos.svg";
        if(item.Language is ScriptLanguage.PowerShell)
            return $"/icons/powershell.svg";
        if(item.Language is ScriptLanguage.Shell)
            return $"/icons/bash.svg";
        if(item.Language is ScriptLanguage.CSharp)
            return $"/icons/csharp.svg";

        return "/icons/javascript.svg";
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Script.ScriptsUpdated -= OnScriptsUpdated;
    }
}