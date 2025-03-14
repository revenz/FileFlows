using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Wizards;
using FileFlows.Client.Shared;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// Libraries page
/// </summary>
public partial class Libraries : ListPage<Guid, Library>
{

    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    /// <inheritdoc />
    public override string ApiUrl => "/api/library";
    /// <summary>
    /// The current item being edited
    /// </summary>
    private Library EditingItem = null;
    /// <summary>
    /// Translation strings
    /// </summary>
    private string lblLastScanned, lblFlow, lblSavings;
    /// <summary>
    /// If the system has libraries created
    /// </summary>
    private bool HasCreatedLibraries = false;
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblLastScanned = Translater.Instant("Labels.LastScanned");
        lblFlow = Translater.Instant("Labels.Flow");
        lblSavings = Translater.Instant("Labels.Savings");
    }

    /// <summary>
    /// Adds an item
    /// </summary>
    private async Task Add()
    {
        // show the AddFileDialog
        Result<Library> result = await ModalService.ShowModal<NewLibraryWizard, Library>(new NewLibraryWizardOptions()
        {
        });
        if(result.Success(out var library))
            await ItemSaved(library);
        // if (result.Success(out var uploadedFile))
        // {
        //     Files.Add(uploadedFile);
        //     file = e.File;
        // }
        // else
        // {
        //     // remove the file from the InputFile
        //     file = null;
        // }
        
        // await Edit(new ()
        // {  
        //     Enabled = true, 
        //     ScanInterval = 3 * 60 * 60, 
        //     FileSizeDetectionInterval = 5,
        //     // UseFingerprinting = false,
        //     // UpdateMovedFiles = true,
        //     Schedule = new string('1', 672)
        // });
    }

    /// <summary>
    /// Duplicate the selected item
    /// </summary>
    private async Task Duplicate()
    {
        Blocker.Show();
        try
        {
            var item = Table.GetSelected()?.Where(x => x.Uid != CommonVariables.ManualLibraryUid)?.FirstOrDefault();
            if (item == null)
                return;
            string url = $"/api/library/duplicate/{item.Uid}";
#if (DEBUG)
            url = "http://localhost:6868" + url;
#endif
            var newItem = await HttpHelper.Post<Library>(url);
            if (newItem is { Success: true })
            {
                await this.Refresh();
                Toast.ShowSuccess(Translater.Instant("Pages.Library.Labels.Duplicated",
                    new { name = newItem.Data.Name }));
            }
            else
            {
                Toast.ShowError(newItem.Body?.EmptyAsNull() ?? "Pages.Library.Labels.FailedToDuplicate");
            }
        }
        finally
        {
            Blocker.Hide();
        }
    }
    
    /// <summary>
    /// Gets the flows
    /// </summary>
    /// <returns>the list of flows</returns>
    private Task<RequestResult<Dictionary<Guid, string>>> GetFlows()
        => HttpHelper.Get<Dictionary<Guid, string>>("/api/flow/basic-list?type=Standard");

    private Dictionary<string, StorageSavedData> StorageSaved = new ();

    /// <inheritdoc />
    protected override async Task<RequestResult<List<Library>>> FetchData()
    {
        StorageSaved =
            (await HttpHelper.Get<List<StorageSavedData>>("/api/statistics/storage-saved-raw"))
            .Data?.ToDictionary(x => x.Library, x => x) ?? new ();
        return await base.FetchData();
    }

    public override async Task<bool> Edit(Library library)
    {
        this.EditingItem = library;
        return await OpenEditor(library);
    }

    private void TemplateValueChanged(object sender, object value) 
    {
        if (value == null)
            return;
        var template = value as Library;
        if (template == null)
            return;
        var editor = sender as Editor;
        if (editor == null)
            return;
        if (editor.Model == null)
            editor.Model = new ExpandoObject();
        IDictionary<string, object> model = editor.Model!;
        
        SetModelProperty(nameof(template.Name), template.Name);
        SetModelProperty(nameof(template.Template), template.Name);
        SetModelProperty(nameof(template.FileSizeDetectionInterval), template.FileSizeDetectionInterval);
        SetModelProperty(nameof(template.Filters), template.Filters);
        SetModelProperty(nameof(template.Extensions), template.Extensions?.ToArray() ?? new string[] { });
        //SetModelProperty(nameof(template.UseFingerprinting), template.UseFingerprinting);
        SetModelProperty(nameof(template.ExclusionFilters), template.ExclusionFilters);
        SetModelProperty(nameof(template.Path), template.Path);
        SetModelProperty(nameof(template.Priority), template.Priority);
        SetModelProperty(nameof(template.ScanInterval), template.ScanInterval);
        //SetModelProperty(nameof(template.ReprocessRecreatedFiles), template.ReprocessRecreatedFiles);
        SetModelProperty(nameof(template.Folders), template.Folders);
        //SetModelProperty(nameof(template.UpdateMovedFiles), template.UpdateMovedFiles);

        editor.TriggerStateHasChanged();
        void SetModelProperty(string property, object value)
        {
            model[property] = value;
        }
    }

    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var saveResult = await HttpHelper.Post<Library>($"{ApiUrl}", model);
            if (saveResult.Success == false)
            {
                Toast.ShowEditorError( Translater.TranslateIfNeeded(saveResult.Body?.EmptyAsNull() ?? "ErrorMessages.SaveFailed"));
                return false;
            }

            await ItemSaved(saveResult.Data);

            return true;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }


    /// <summary>
    /// Called after an item is saved
    /// </summary>
    /// <param name="library">the saved library</param>
    private async Task ItemSaved(Library library)
    {
        // if ((Profile.ConfigurationStatus & ConfigurationStatus.Libraries) !=
        //     ConfigurationStatus.Libraries)
        // {
        //     // refresh the app configuration status
        //     await feService.Refresh();
        // }

        int index = this.Data.FindIndex(x => x.Uid == library.Uid);
        if (index < 0)
            this.Data.Add(library);
        else
            this.Data[index] = library;

        await this.Load(library.Uid);
    }

    private string TimeSpanToString(Library lib)
    {
        if (lib.Uid == CommonVariables.ManualLibraryUid)
            return string.Empty;
        if (lib.LastScanned.Year < 2001)
            return Translater.Instant("Times.Never");

        if (lib.LastScannedAgo.TotalMinutes < 1)
            return Translater.Instant("Times.SecondsAgo", new { num = (int)lib.LastScannedAgo.TotalSeconds });
        if (lib.LastScannedAgo.TotalHours < 1 && lib.LastScannedAgo.TotalMinutes < 120)
            return Translater.Instant("Times.MinutesAgo", new { num = (int)lib.LastScannedAgo.TotalMinutes });
        if (lib.LastScannedAgo.TotalDays < 1)
            return Translater.Instant("Times.HoursAgo", new { num = (int)Math.Round(lib.LastScannedAgo.TotalHours) });
        else
            return Translater.Instant("Times.DaysAgo", new { num = (int)lib.LastScannedAgo.TotalDays });
    }

    private async Task Rescan()
    {
        var uids = Table.GetSelected()?.Where(x => x.Uid != CommonVariables.ManualLibraryUid)
            ?.Select(x => x.Uid)?.ToArray() ?? new System.Guid[] { };
        if (uids.Length == 0)
            return; // nothing to rescan

        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var result = await HttpHelper.Put($"{ApiUrl}/rescan", new ReferenceModel<Guid> { Uids = uids });
            if (result.Success == false)
                return;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Reprocess all files in a library
    /// </summary>
    private async Task Reprocess()
    {
        var uids = Table.GetSelected()?.Where(x => x.Uid != CommonVariables.ManualLibraryUid)
            ?.Select(x => x.Uid)?.ToArray() ?? new System.Guid[] { };
        if (uids.Length == 0)
            return; // nothing to rescan

        if (await Confirm.Show("Pages.Libraries.Messages.Reprocess.Title",
                "Pages.Libraries.Messages.Reprocess.Message", defaultValue: false) == false)
            return;

        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var result = await HttpHelper.Put($"{ApiUrl}/reprocess", new ReferenceModel<Guid> { Uids = uids });
            if (result.Success == false)
                return;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }
    /// <summary>
    /// Reset all files in a library
    /// </summary>
    private async Task Reset()
    {
        var uids = Table.GetSelected()?.Where(x => x.Uid != CommonVariables.ManualLibraryUid)
            ?.Select(x => x.Uid)?.ToArray() ?? new System.Guid[] { };
        if (uids.Length == 0)
            return; // nothing to rescan

        if (await Confirm.Show("Pages.Libraries.Messages.Reset.Title",
                "Pages.Libraries.Messages.Reset.Message", defaultValue: false) == false)
            return;

        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var result = await HttpHelper.Put($"{ApiUrl}/reset", new ReferenceModel<Guid> { Uids = uids });
            if (result.Success == false)
                return;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    public override async Task Delete()
    {
        var uids = Table.GetSelected()?.Where(x => x.Uid != CommonVariables.ManualLibraryUid)?
            .Select(x => x.Uid)?.ToArray() ?? new System.Guid[] { };
        if (uids.Length == 0)
            return; // nothing to delete
        var confirmResult = await Confirm.Show("Labels.Delete",
            Translater.Instant("Pages.Libraries.Messages.DeleteConfirm", new { count = uids.Length })
        );
        if (confirmResult == false)
            return; // rejected the confirm

        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var deleteResult = await HttpHelper.Delete(ApiUrl, new ReferenceModel<Guid> { Uids = uids });
            if (deleteResult.Success == false)
            {
                if(Translater.NeedsTranslating(deleteResult.Body))
                    Toast.ShowError( Translater.Instant(deleteResult.Body));
                else
                    Toast.ShowError( Translater.Instant("ErrorMessages.DeleteFailed"));
                return;
            }

            this.Data = this.Data.Where(x => uids.Contains(x.Uid) == false).ToList();

            await PostDelete();
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Get priority icon
    /// </summary>
    /// <param name="library">the library</param>
    /// <returns>the priority icon class</returns>
    private string GetPriorityIcon(Library library)
    {
        switch (library.Priority)
        {
            case ProcessingPriority.Highest:
                return "fas fa-angle-double-up";
            case ProcessingPriority.High:
                return "fas fa-angle-up";
            case ProcessingPriority.Low:
                return "fas fa-angle-down";
            case ProcessingPriority.Lowest:
                return "fas fa-angle-double-down";
            default:
                return "fas fa-folder";
        }
    }

    /// <summary>
    /// Gets the storage saved
    /// </summary>
    /// <param name="libraryName">the name of the library</param>
    /// <param name="storageSavedData">the savings if found</param>
    /// <returns>if the storage savings were in the dictionary</returns>
    private bool GetStorageSaved(string libraryName, out StorageSavedData storageSavedData)
        => StorageSaved.TryGetValue(libraryName, out storageSavedData);
    
    
    /// <summary>
    /// we only want to do the sort the first time, otherwise the list will jump around for the user
    /// </summary>
    private List<Guid> initialSortOrder;
    
    /// <inheritdoc />
    public override Task PostLoad()
    {
        // Data.RemoveAll(x => x.Uid == CommonVariables.ManualLibraryUid);
        HasCreatedLibraries = Data?.Any(x => x.Uid != CommonVariables.ManualLibraryUid) == true;
        
        if (initialSortOrder == null)
        {
            Data = Data?.OrderByDescending(x => x.Enabled)?.ThenBy(x => x.Name)
                ?.ToList();
            initialSortOrder = Data?.Select(x => x.Uid)?.ToList();
        }
        else
        {
            Data = Data?.OrderBy(x => initialSortOrder.Contains(x.Uid) ? initialSortOrder.IndexOf(x.Uid) : 1000000)
                .ThenBy(x => x.Name)
                ?.ToList();
        }
        return base.PostLoad();
    }

    /// <summary>
    /// Opens the flow in the editor
    /// </summary>
    /// <param name="flowUid">the UID of the flow</param>
    private void OpenFlow(Guid? flowUid)
    {
        if (flowUid == null || Profile.HasRole(UserRole.Flows) == false)
            return;

        NavigationManager.NavigateTo($"/flows/{flowUid}");
    }
}

