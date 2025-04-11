using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Wizards;
using FileFlows.Client.Shared;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// Libraries page
/// </summary>
public partial class Libraries : ListPage<Guid, LibraryListModel>, IDisposable
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
    private LibraryListModel EditingItem = null;
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
        Layout.SetInfo(Translater.Instant("Pages.Libraries.Title"), "fas fa-folder");
        Profile = feService.Profile.Profile;
        base.OnInitialized(false);
        lblLastScanned = Translater.Instant("Labels.LastScanned");
        lblFlow = Translater.Instant("Labels.Flow");
        lblSavings = Translater.Instant("Labels.Savings");
        feService.Library.LibrariesUpdated += LibrariesUpdated;
        Data = feService.Library.Libraries;
        SortData();
    }

    /// <summary>
    /// Libraries updated
    /// </summary>
    /// <param name="data">the updated data</param>
    private void LibrariesUpdated(List<LibraryListModel> data)
    {
        Data = data;
        SortData();
        StateHasChanged();
    }

    /// <summary>
    /// Adds an item
    /// </summary>
    private async Task Add()
        => await ModalService.ShowModal<NewLibraryWizard, Library>(new NewLibraryWizardOptions());

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
                feService.Notifications.ShowSuccess(Translater.Instant("Pages.Library.Labels.Duplicated",
                    new { name = newItem.Data.Name }));
            }
            else
            {
                feService.Notifications.ShowError(newItem.Body?.EmptyAsNull() ?? "Pages.Library.Labels.FailedToDuplicate");
            }
        }
        finally
        {
            Blocker.Hide();
        }
    }
    
    public override async Task<bool> Edit(LibraryListModel library)
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

    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var saveResult = await HttpHelper.Post<Library>($"{ApiUrl}", model);
            if (saveResult.Success == false)
            {
                feService.Notifications.ShowError( Translater.TranslateIfNeeded(saveResult.Body?.EmptyAsNull() ?? "ErrorMessages.SaveFailed"));
                return false;
            }

            return true;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }


    private string TimeSpanToString(LibraryListModel lib)
    {
        if (lib.Uid == CommonVariables.ManualLibraryUid)
            return string.Empty;
        if (lib.LastScanned.Year < 2001)
            return Translater.Instant("Times.Never");

        var libLastScannedAgo = DateTime.UtcNow.Subtract(lib.LastScanned);

        if (libLastScannedAgo.TotalMinutes < 1)
            return Translater.Instant("Times.SecondsAgo", new { num = (int)libLastScannedAgo.TotalSeconds });
        if (libLastScannedAgo.TotalHours < 1 && libLastScannedAgo.TotalMinutes < 120)
            return Translater.Instant("Times.MinutesAgo", new { num = (int)libLastScannedAgo.TotalMinutes });
        if (libLastScannedAgo.TotalDays < 1)
            return Translater.Instant("Times.HoursAgo", new { num = (int)Math.Round(libLastScannedAgo.TotalHours) });
        else
            return Translater.Instant("Times.DaysAgo", new { num = (int)libLastScannedAgo.TotalDays });
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
                    feService.Notifications.ShowError( Translater.Instant(deleteResult.Body));
                else
                    feService.Notifications.ShowError( Translater.Instant("ErrorMessages.DeleteFailed"));
                return;
            }

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
    private string GetPriorityIcon(LibraryListModel library)
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
    /// we only want to do the sort the first time, otherwise the list will jump around for the user
    /// </summary>
    private List<Guid> initialSortOrder;

    /// <summary>
    /// Sorts the data
    /// </summary>
    private void SortData()
    {
        // Data.RemoveAll(x => x.Uid == CommonVariables.ManualLibraryUid);
        HasCreatedLibraries = Data.Any(x => x.Uid != CommonVariables.ManualLibraryUid) == true;
        
        if (initialSortOrder == null)
        {
            Data = Data.OrderByDescending(x => x.Enabled)
                .ThenByDescending(x => (int)x.Priority)
                .ThenBy(x => x.Name)
                .ToList();
            initialSortOrder = Data.Select(x => x.Uid).ToList();
        }
        else
        {
            Data = Data.OrderBy(x => initialSortOrder.Contains(x.Uid) ? initialSortOrder.IndexOf(x.Uid) : 1000000)
                .ThenByDescending(x => (int)x.Priority)
                .ThenBy(x => x.Name)
                .ToList();
        }
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

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Library.LibrariesUpdated -= LibrariesUpdated;
    }
}

