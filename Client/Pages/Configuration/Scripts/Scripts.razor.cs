using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

using FileFlows.Client.Components;

/// <summary>
/// Page for processing nodes
/// </summary>
public partial class Scripts : ListPage<Guid, Script>
{
    public override string ApiUrl => "/api/script";

    const string FileFlowsServer = "FileFlowsServer";

    private string TableIdentifier => "Scripts-" + this.SelectedType;
    
    private FlowSkyBox<ScriptType> Skybox;

    private Script EditingItem = null;
    [Inject] public IJSRuntime jsRuntime { get; set; }
    
    private List<Script> DataFlow = new();
    private List<Script> DataSystem = new();
    private List<Script> DataShared = new();
    private ScriptType SelectedType = ScriptType.Flow;

    private string lblTitle, lblUpdateScripts, lblUpdatingScripts, lblInUse, lblReadOnly, lblUpdateAvailable,
        lblFileDisplayName ,lblFileDisplayNameDescription;

    /// <summary>
    /// Gets or sets the instance of the ScriptBrowser
    /// </summary>
    private RepositoryBrowser ScriptBrowser { get; set; }

    /// <summary>
    /// The language picker dialog
    /// </summary>
    private ScriptLanguagePicker LanguagePicker;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblTitle = Translater.Instant("Pages.Scripts.Title");
        lblUpdateScripts = Translater.Instant("Pages.Scripts.Buttons.UpdateAllScripts");
        lblUpdatingScripts = Translater.Instant("Pages.Scripts.Labels.UpdatingScripts");
        lblFileDisplayName = Translater.Instant("Dialogs.ScriptLanguage.Labels.FileDisplayName");
        lblFileDisplayNameDescription = Translater.Instant("Dialogs.ScriptLanguage.Labels.FileDisplayNameDescription");
        lblInUse = Translater.Instant("Labels.InUse");
        lblReadOnly = Translater.Instant("Labels.ReadOnly");
        lblUpdateAvailable = Translater.Instant("Pages.Scripts.Labels.UpdateAvailable");
    }


    private async Task Add()
    {
        ScriptLanguage language;
        if (SelectedType == ScriptType.Shared)
            language = ScriptLanguage.JavaScript;
        else
        {
            bool displayNameScript = SelectedType == ScriptType.System &&
                                     Data.Any(x => x.Name == CommonVariables.FILE_DISPLAY_NAME) == false;
            var result = await LanguagePicker.Show(displayNameScript);
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
        
        if ((int)language == 99)
        {
            // special case for FILE_DISPLAY_NAME
            script.Name = CommonVariables.FILE_DISPLAY_NAME;
            script.Language = ScriptLanguage.JavaScript;
            script.Code = DEFAULT_FILE_DISPLAY_NAME_SCRIPT;
        }

        await Edit(script);
    }


    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var saveResult = await HttpHelper.Post<Script>($"{ApiUrl}", model);
            if (saveResult.Success == false)
            {
                Toast.ShowEditorError(saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
                return false;
            }

            int index = this.Data.FindIndex(x => x.Uid == saveResult.Data.Uid);
            if (index < 0)
                this.Data.Add(saveResult.Data);
            else
                this.Data[index] = saveResult.Data;
            await this.Load(saveResult.Data.Uid);

            return true;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
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
            Toast.ShowError(Translater.Instant("Pages.Script.Messages.FailedToExport"));
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
        var idResult = await ImportDialog.Show("js");//, "ps1", "cs", "bat", "sh");
        string js = idResult.content;
        if (string.IsNullOrEmpty(js))
            return;

        Blocker.Show();
        try
        {
            var newItem = await HttpHelper.Post<Script>("/api/script/import?filename=" + UrlEncoder.Create().Encode(idResult.filename) + "&type=" + SelectedType, js);
            if (newItem != null && newItem.Success)
            {
                await this.Refresh();
                Toast.ShowSuccess(Translater.Instant("Pages.Scripts.Messages.Imported",
                    new { name = newItem.Data.Name }));
            }
            else
            {
                Toast.ShowError(newItem.Body?.EmptyAsNull() ?? "Invalid script");
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
                await this.Refresh();
                Toast.ShowSuccess(Translater.Instant("Pages.Script.Messages.Duplicated",
                    new { name = newItem.Data.Name }));
            }
            else
            {
                Toast.ShowError(newItem.Body?.EmptyAsNull() ?? "Failed to duplicate");
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
            Toast.ShowError("Pages.Scripts.Messages.DeleteUsed");
            return;
        }

        await base.Delete();
        await Refresh();
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
        this.DataFlow = this.Data.Where(x => x.Type == ScriptType.Flow).ToList();
        this.DataSystem = this.Data.Where(x => x.Type == ScriptType.System).ToList();
        this.DataShared = this.Data.Where(x => x.Type == ScriptType.Shared).ToList();
        foreach (var script in this.Data)
        {
            if (script.Code?.StartsWith("// path: ") == true)
                script.Code = Regex.Replace(script.Code, @"^\/\/ path:(.*?)$", string.Empty, RegexOptions.Multiline).Trim();
        }
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
        bool result = await ScriptBrowser.Open();//this.SelectedType);
        if (result)
            await this.Refresh();
    }

    async Task Update()
    {
        var uids = Table.GetSelected()?.Where(x => string.IsNullOrEmpty(x.Path) == false)?.Select(x => x.Uid)?.ToArray() ?? new Guid[] { };
        if (uids?.Any() != true)
        {
            Toast.ShowWarning("Pages.Scripts.Messages.NoRepositoryScriptsToUpdate");
            return;
        }

        Blocker.Show("Pages.Scripts.Labels.UpdatingScripts");
        this.StateHasChanged();
        Data.Clear();
        try
        {
            var result = await HttpHelper.Post($"/api/repository/update-specific-scripts", new ReferenceModel<Guid> { Uids = uids });
            if (result.Success)
                await Refresh();
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
            await Refresh();
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
        if (item.Name == CommonVariables.FILE_DISPLAY_NAME)
            return "fas fa-signature";
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
        //return "fas fa-scroll";

    }


    private const string DEFAULT_FILE_DISPLAY_NAME_SCRIPT = @"
function getDisplayName(fullName, relativePath, libraryName) 
{
    if(/(movies|tv)/i.test(libraryName) === false)
        return relativePath;

    let extension = relativePath.substring(relativePath.lastIndexOf('.') + 1);

    if(/([/])[\w\d]+\.[\w\d]{2,6}$/i.test(relativePath))
        relativePath = relativePath.substring(0, relativePath.lastIndexOf('/'));
    else if(/([\\])[\w\d]+\.[\w\d]{2,6}$/i.test(relativePath))
        relativePath = relativePath.substring(0, relativePath.lastIndexOf('\\'));

    let hdr = /[\.\s\-]hdr/i.test(relativePath) === true;
    let tenbit = /[\.\s\-]10(\-)?bit/i.test(relativePath) === true;
    let twelvebit = /[\.\s\-]12(\-)?bit/i.test(relativePath) === true;
    let resolution = ((resolutionMatch = /[\.\s\-](1080p|1080i|720p|420p|4k)/i.exec(relativePath)) && resolutionMatch[1]) || '';
    let year = ((yearMatch = /[\.\s\-]((19|20)[\d]{2})[\.\s\-]/.exec(relativePath)) && yearMatch[1]) || '';

    let yearIndex = year ? relativePath.indexOf(year) : -1;
    let resolutionIndex = resolution ? relativePath.indexOf(resolution) : -1;

    if(yearIndex > 0 || resolutionIndex > 0)
    {
      let closest = Math.min(yearIndex === -1 ? 9999999 : yearIndex, resolutionIndex === -1 ? 9999999 : resolutionIndex);
      relativePath = relativePath.substring(0, closest);
      relativePath = relativePath.replace(/\./g, ' ').replace(/.*[\/\\]/, '').trim();
    }

    relativePath = relativePath.replace(/(S\d+E\d+)/, ' - $1 - ');
    if(relativePath.indexOf(' - .') > 0){
        relativePath = relativePath.substring(0, relativePath.indexOf(' - .'));
        relativePath = relativePath.replace(/\./g, ' ').replace(/.*[\/\\]/, '').trim();
    }
    relativePath = relativePath.replace(/\s{2,}/g, ' ');
    
    let additional = [];
    if(year)
      additional.push(year);
    if(resolution)
      additional.push(resolution);
    if(tenbit)
      additional.push('10-Bit');
    else if(twelvebit)
      additional.push('12-Bit');
    if(hdr)
      additional.push('HDR');

    relativePath = relativePath.trim();

    if(relativePath.trim().endsWith(' -'))
      relativePath = relativePath.substring(0, relativePath.length - 2);

    if(additional.length)
      relativePath += ' - ' + additional.join(' / ');

    return relativePath + '.' + extension;
}
";
}