using System.Threading;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using Microsoft.AspNetCore.Components;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Helpers;
using FileFlows.Client.Services.Frontend;
using FileFlows.Client.Shared;

namespace FileFlows.Client.Pages;

/// <summary>
/// List Page
/// </summary>
/// <typeparam name="U">The type of Unique Identifier</typeparam>
/// <typeparam name="T">The type bound in the list</typeparam>
public abstract class ListPage<U, T> : ComponentBase where T : IUniqueObject<U>
{
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] public NavigationManager NavigationManager { get; set; }
    /// <summary>
    /// Gets or sets the table instance
    /// </summary>
    protected FlowTable<T> Table { get; set; }
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the editor
    /// </summary>
    [CascadingParameter] public Editor Editor { get; set; }
    /// <summary>
    /// Gets or sets the Layout
    /// </summary>
    [CascadingParameter] public MainLayout Layout { get; set; }
    /// <summary>
    /// Translations
    /// </summary>
    public string lblAdd, lblEdit, lblDelete, lblDeleting, lblRefresh;
    

    /// <summary>
    /// Gets the API URL for the list
    /// </summary>
    public abstract string ApiUrl { get; }
    /// <summary>
    /// If this component needs rendering
    /// </summary>
    private bool _needsRendering;

    /// <summary>
    /// Gets or sets if the list has loaded
    /// </summary>
    protected bool Loaded { get; set; }
    /// <summary>
    /// Gets or sets if there is data
    /// </summary>
    protected bool HasData { get; set; }

    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] protected FrontendService feService { get; set; }
    
    /// <summary>
    /// Gets the profile
    /// </summary>
    protected Profile Profile { get; set; }

    private List<T> _Data = new ();
    
    /// <summary>
    /// Gets the data
    /// </summary>
    public List<T> Data
    {
        get => _Data;
        set => _Data = value ?? new ();
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Profile = feService.Profile.Profile;
        if (Licensed() == false)
        {
            NavigationManager.NavigateTo("/");
            return;
        }
        OnInitialized(true);
    }

    protected void OnInitialized(bool load)
    {
        lblAdd = Translater.Instant("Labels.Add");
        lblEdit = Translater.Instant("Labels.Edit");
        lblDelete = Translater.Instant("Labels.Delete");
        lblDeleting = Translater.Instant("Labels.Deleting");
        lblRefresh = Translater.Instant("Labels.Refresh");

        if(load)
            _ = Load(default);
    }

    public virtual async Task Refresh(bool showBlocker = true) => await Load(default, showBlocker);

    public virtual string FetchUrl => ApiUrl;

    /// <summary>
    /// Called after the data is loaded
    /// </summary>
    public virtual Task PostLoad()
        => Task.CompletedTask;

    /// <summary>
    /// Called directly after the data is fetched from the server, and before it is bound to the table
    /// This happens before "PostLoad"
    /// </summary>
    /// <param name="data">the data from the server</param>
    /// <returns>a task to await</returns>
    public virtual Task<List<T>> PostLoadGotData(List<T> data)
        => Task.FromResult(data);
    
    /// <summary>
    /// Waits for a render to occur
    /// </summary>
    protected async Task WaitForRender()
    {
        _needsRendering = true;
        StateHasChanged();
        while (_needsRendering)
        {
            await Task.Delay(50);
        }
    }
    
    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        _needsRendering = false;
    }

    private SemaphoreSlim fetching = new(1);

    /// <summary>
    /// Sets the table data, virtual so a filter can be set if needed
    /// </summary>
    /// <param name="data">the data to set</param>
    protected virtual void SetTableData(List<T> data) => Table?.SetData(data, clearSelected: false);

    public virtual async Task Load(U selectedUid, bool showBlocker = true)
    {
        if(showBlocker)
            Blocker.Show("Loading Data");
        await this.WaitForRender();
        try
        {
            await fetching.WaitAsync();
            var result = await FetchData();
            if (result.Success)
            {
                this.Data = await PostLoadGotData(result.Data);
                if (Table != null)
                {
                    SetTableData(this.Data);
                    var item = this.Data.FirstOrDefault(x => x.Uid.Equals(selectedUid));
                    if (item != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            // need a delay here since setdata and the inner works of FlowTable will clear this without it
                            await Task.Delay(50);
                            Table.SelectItem(item);
                        });
                    }
                }
            }
            await PostLoad();
        }
        finally
        {
            fetching.Release();
            HasData = this.Data?.Any() == true;
            this.Loaded = true;
            if(showBlocker)
                Blocker.Hide();
            await this.WaitForRender();
        }
    }

    protected virtual Task<RequestResult<List<T>>> FetchData()
    {
        return HttpHelper.Get<List<T>>(FetchUrl);
    }


    /// <summary>
    /// Event called when a user double-clicks on a item
    /// </summary>
    /// <param name="item">the item that was double-clicked</param>
    protected async Task OnDoubleClick(T item)
    {
        await Edit(item);
    }


    /// <summary>
    /// Edits an item
    /// </summary>
    public async Task Edit()
    {
        var items = Table?.GetSelected()?.ToList();
        if (items?.Any() != true)
            return;
        var selected = items.First();
        if (selected == null)
            return;
        var changed = await Edit(selected);
        if (changed)
            await this.Load(selected.Uid);
    }

    /// <summary>
    /// Edit a specific item
    /// </summary>
    /// <param name="item">the item to edit</param>
    /// <returns>true if the item was changed</returns>
    public abstract Task<bool> Edit(T item);

    /// <summary>
    /// Shows an error in a toast
    /// </summary>
    /// <param name="result">The result with the error</param>
    /// <param name="defaultMessage">the default message</param>
    /// <typeparam name="V">The type of RequestResult</typeparam>
    public void ShowEditHttpError<V>(RequestResult<V> result, string defaultMessage = "ErrorMessage.NotFound")
    {
        feService.Notifications.ShowError(
            result.Success || string.IsNullOrEmpty(result.Body) ? Translater.Instant(defaultMessage) : Translater.TranslateIfNeeded(result.Body),
            duration: 60_000
        );
    }
    
    /// <summary>
    /// Tests if the user is licensed for this page
    /// </summary>
    /// <returns>true if they are licensed</returns>
    protected virtual bool Licensed() => true;


    /// <summary>
    /// Enables or disabled the item
    /// </summary>
    /// <param name="enabled"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public EventCallback Enable(bool enabled, T item)
    {
        Task.Run(async () =>
        {
            Blocker.Show();
            StateHasChanged();
            Data.Clear();
            try
            {
                await HttpHelper.Put<T>($"{ApiUrl}/state/{item.Uid}?enable={enabled}");
            }
            finally
            {
                Blocker.Hide();
                this.StateHasChanged();
            }
        });
        return EventCallback.Empty;
    }

    /// <summary>
    /// Gets the delete message
    /// </summary>
    protected virtual string DeleteMessage => "Labels.DeleteItems";
    /// <summary>
    /// Gets the delete URL
    /// </summary>
    protected virtual string DeleteUrl => ApiUrl;

    public virtual async Task Delete()
    {
        var uids = Table.GetSelected()?.Select(x => x.Uid)?.ToArray() ?? new U[] { };
        if (uids.Length == 0)
            return; // nothing to delete
        if (await Confirm.Show("Labels.Remove",
            Translater.Instant(DeleteMessage, new { count = uids.Length })) == false)
            return; // rejected the confirm

        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var deleteResult = await HttpHelper.Delete(DeleteUrl, new ReferenceModel<U> { Uids = uids });
            if (deleteResult.Success == false)
            {
                if(Translater.NeedsTranslating(deleteResult.Body))
                    feService.Notifications.ShowError( Translater.Instant(deleteResult.Body));
                else
                    feService.Notifications.ShowError( Translater.Instant("ErrorMessages.DeleteFailed"));
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

    protected async virtual Task PostDelete()
    {
        await Task.CompletedTask;
    }

    protected async Task Revisions()
    {
        var items = Table.GetSelected()?.ToList();
        if (items?.Any() != true)
            return;
        var selected = items.First();
        if (selected == null)
            return;
        Guid guid;
        if (selected is RevisionedObject ro)
            guid = ro.RevisionUid;
        else if (selected.Uid is Guid sGuid)
            guid = sGuid;
        else
            return;
        
        bool changed = await RevisionExplorer.Instance.Show(guid, "Revisions");
        if (changed)
            await Load(selected.Uid);
    }
    
    /// <summary>
    /// Shows the audit log
    /// </summary>
    protected async Task AuditLog()
    {
        if (Profile.LicensedFor(LicenseFlags.Auditing) == false)
            return;
        
        var selected = Table.GetSelected().FirstOrDefault();
        if (selected == null)
            return;
        if(selected.Uid is Guid uid)
            await AuditHistory.Instance.Show(uid, GetAuditTypeName());
    }

    /// <summary>
    /// Gets the audit type name
    /// </summary>
    /// <returns>the audit type name</returns>
    protected virtual string GetAuditTypeName()
        => typeof(T).FullName;
    
    /// <summary>
    /// Humanizes a date, eg 11 hours ago
    /// </summary>
    /// <param name="dateUtc">the date</param>
    /// <returns>the humanized date</returns>
    protected string DateString(DateTime? dateUtc)
    {
        if (dateUtc == null) return string.Empty;
        if (dateUtc.Value.Year < 2020) return string.Empty; // fixes 0000-01-01 issue
        // var localDate = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day, date.Value.Hour,
        //     date.Value.Minute, date.Value.Second);

        return FormatHelper.HumanizeDate(dateUtc.Value);
    }

}