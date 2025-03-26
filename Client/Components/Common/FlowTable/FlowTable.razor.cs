using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Timers;
using BlazorContextMenu;
using FileFlows.Client.Services.Frontend;

namespace FileFlows.Client.Components.Common;

public abstract class FlowTableBase: ComponentBase
{
    protected List<FlowTableButton> Buttons = new();
    protected List<object> ToolbarItems = new();
    public delegate void SelectionChangedEvent(List<object> items);
    public event SelectionChangedEvent SelectionChanged;
    
    internal void AddButton(FlowTableButton button)
    {
        if (Buttons.Contains(button) == false)
            Buttons.Add(button);
        if (ToolbarItems.Contains(button) == false)
            ToolbarItems.Add(button);
    }

    internal void AddButtonSeperator(FlowTableButtonSeparator separator)
    {
        Buttons.Add(null);
        if (ToolbarItems.Contains(separator) == false)
            ToolbarItems.Add(separator);
    }
    protected void NotifySelectionChanged(List<object> selectedItems)
    {
        if (SelectionChanged != null)
            SelectionChanged(new (selectedItems)); // we want a clone of the list, not one they can modify 
    }

}

public partial class FlowTable<TItem>: FlowTableBase,IDisposable, INotifyPropertyChanged where TItem : notnull
{
    private FlowContextMenu TableContextMenu { get; set; }
    
    private readonly string Uid = Guid.NewGuid().ToString();
    private readonly string ContextMenuUid = "ctxMenu-" + Guid.NewGuid();
    private Dictionary<TItem, string> DataDictionary;
    private List<TItem> _Data;

    [Inject] private IBlazorContextMenuService ContextMenuService { get; set; }

    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }

    /// <summary>
    /// Gets or sets if the pager should be shown
    /// </summary>
    [Parameter] public bool ShowPager { get; set; }

    /// <summary>
    /// Gets if the pager is visible
    /// </summary>
    public bool PagerVisible => ShowPager && TotalItems > feService.PageSize;

    /// <summary>
    /// Gets or sets the callback for the filter
    /// </summary>
    [Parameter] public EventCallback<FilterEventArgs> OnFilter { get; set; }
    
    /// <summary>
    /// Gets or sets a disable column function 
    /// </summary>
    [Parameter] public Func<TItem, bool> DisableColumn { get; set; } 

    private int _TotalItems;


#pragma warning disable BL0007
    /// <summary>
    /// Gets or sets the total items, needed for the pager to know how many pages to show
    /// </summary>
    [Parameter]
    public int TotalItems
    {
        get => _TotalItems;
        set
        {
            if (_TotalItems != value)
            {
                _TotalItems = value;
                OnPropertyChanged(nameof(TotalItems));
            }
        }
    }
#pragma warning restore BL0007

    /// <summary>
    /// Gets or sets callback when the page size is changed
    /// </summary>
    [Parameter] public EventCallback<int> OnPageSizeChange { get; set; }
    
    /// <summary>
    /// Gets or sets callback when the page is changed
    /// </summary>
    [Parameter] public EventCallback<int> OnPageChange { get; set; }

#pragma warning disable BL0007
    /// <summary>
    /// Gets or sets the data
    /// </summary>
    [Parameter]
    public List<TItem> Data // the original data, not filtered
    {
        get => this._Data;
        set
        {
            if (this._Data == value)
                return;
            SetData(value, clearSelected: false);
        }
    }
#pragma warning restore BL0007

    /// <summary>
    /// Gets or sets a table identifier for saving/loading the column information
    /// </summary>
    [Parameter] public string TableIdentifier { get; set; }

    /// <summary>
    /// Sets the data
    /// </summary>
    /// <param name="value">the data to set</param>
    /// <param name="clearSelected">if the selected items should be cleared</param>
    /// <param name="filter">[Optional] text for the filter</param>
    public void SetData(List<TItem> value, bool clearSelected = true, string filter = null)
    {
        this._FilterText = filter ?? string.Empty;
        this._Data = value ?? new();
        var jsonOptions = new System.Text.Json.JsonSerializerOptions()
        {   
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        this.DataDictionary = this._Data.ToDictionary(x => x, x =>
        {
            var result = JsonSerializer.Serialize(x, jsonOptions);  
            return result.ToLowerExplicit();
        });
        if(string.IsNullOrEmpty(filter)) // if we pass in filter, the data is already filtered
            FilterData(clearSelected : clearSelected).Wait();
        else
            this.DisplayData = this.DataDictionary; // need to update the data
        
        // need to reselect the items here!
        var keys = this.SelectedItems.Select(x =>
        {
            if (x is IUniqueObject<Guid> unique)
                return unique.Uid as Guid?;
            return null;
        }).Where(x => x != null).Select(x => x!.Value).ToList();
        
        var selection = this.Data.Where(x =>
        {
            if (x is IUniqueObject<Guid> unique && keys.Contains(unique.Uid))
                return true;
            return false;
        }).ToList();
        this.SelectedItems.AddRange(selection);
    }

    /// <summary>
    /// Gets or sets the minimum width of the table
    /// </summary>
    [Parameter] public string MinWidth { get; set; }

    private ElementReference eleFilter { get; set; }

    /// <summary>
    /// Gets or sets the callback when an item is double clicked
    /// </summary>
    [Parameter] public EventCallback<TItem> DoubleClick { get; set; }

    /// <summary>
    /// Gets or sets if the head row should be hidden
    /// </summary>
    [Parameter] public bool HideHead { get; set; }
    
    /// <summary>
    /// Gets or sets if the toolbar should be hidden
    /// </summary>
    [Parameter] public bool HideToolbar { get; set; }
    
    /// <summary>
    /// Gets or sets if there is no filter available
    /// </summary>
    [Parameter] public bool NoFilter { get; set; }
    
    /// <summary>
    /// Gets or sets if clicking will clear existing selected items
    /// </summary>
    [Parameter] public bool DontClearOnClick { get; set; }
    
    /// <summary>
    /// Gets or sets force selections that the user cannot de-select
    /// </summary>
    [Parameter] public List<TItem> ForcedSelection { get; set; }
    
    private Dictionary<TItem, string> DisplayData { get; set; } = new ();

    private readonly List<TItem> SelectedItems = new ();

    private string CurrentFilter = string.Empty;

    private string _FilterText = string.Empty;

    private Timer filterTimer;
    
    private string FilterText
    {
        get => _FilterText;
        set
        {
            if (_FilterText?.EmptyAsNull() == value?.EmptyAsNull())
                return;
            _FilterText = value ?? string.Empty;
            // debounce the filter so if the user is still typing we only filter when they have finished
            DisposeFilterTimer();
            filterTimer = new(500);
            filterTimer.Elapsed += FilterTimerOnElapsed;
            filterTimer.Enabled = true;
            filterTimer.Start();
        }
    }

    private async void FilterTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        DisposeFilterTimer();
        await FilterData();
    }

    private void DisposeFilterTimer()
    {
        if (filterTimer == null)
            return;
        filterTimer.Elapsed -= FilterTimerOnElapsed;
        filterTimer.Dispose();
        filterTimer = null;
    }

    /// <summary>
    /// Gets or sets the selection mode for the table
    /// </summary>
    [Parameter] public SelectionMode Selection { get; set; }

    /// <summary>
    /// Gets or sets the toolbar of the table
    /// </summary>
    [Parameter] public RenderFragment ToolBar { get; set; }
    
    /// <summary>
    /// Gets or sets the columns for the table
    /// </summary>
    [Parameter] public RenderFragment Columns { get; set; }

    /// <summary>
    /// The JavaScript runtime
    /// </summary>
    [Inject] IJSRuntime jsRuntime{ get; set; }
    [Inject] IHotKeysService HotKeyService { get; set; }
    
    /// <summary>
    /// Gets or sets the pager
    /// </summary>
    public FlowPager<TItem> Pager { get; set; }

    List<FlowTableColumn<TItem>> ColumnList = new ();

    private TItem LastSelected;


    private string lblFilterPlaceholder, lblFilter;

    public IEnumerable<TItem> GetSelected() => new List<TItem>(this.SelectedItems); // clone the list, dont give them the actual one

    private string FlowTableHotkey;

    /// <summary>
    /// Sets the selected items
    /// </summary>
    /// <param name="items">the items</param>
    public void SetSelected(IEnumerable<TItem> items)
    {
        SelectedItems.Clear();
        if(items != null)
            SelectedItems.AddRange(items);
    }

    protected override void OnInitialized()
    {
        FlowTableHotkey = Guid.NewGuid().ToString();
        lblFilterPlaceholder = Translater.Instant("Labels.FilterPlaceholder");
        lblFilter = Translater.Instant("Labels.Filter");
        HotKeyService.RegisterHotkey(FlowTableHotkey, "/", callback: () =>
        {
            Task.Run(async () =>
            {
                bool editorOpen = await jsRuntime.InvokeAsync<int>("eval", "document.querySelectorAll('.editor-wrapper').length") > 0;
                if (editorOpen)
                    return;
                await eleFilter.FocusAsync();
            });
        });
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        //if (firstRender)
        {
            Task.Run(async () =>
            {
                await Task.Delay(500);
                await jsRuntime.InvokeVoidAsync("ff.resizableTable", this.Uid, this.TableIdentifier);
            });
        }

        return base.OnAfterRenderAsync(firstRender);
    }

    public void Dispose()
    {
        HotKeyService.DeregisterHotkey(FlowTableHotkey);
    }

    internal void AddColumn(FlowTableColumn<TItem> col)
    {
        if (ColumnList.Contains(col) == false)
            ColumnList.Add(col);
    }


    public virtual void SelectAll(ChangeEventArgs e)
    {
        bool @checked = e.Value as bool? == true;
        this.SelectedItems.Clear();
        if (@checked && this.DisplayData?.Any() == true)
            this.SelectedItems.AddRange(this.DisplayData.Keys);
        this.NotifySelectionChanged();
    }

    internal Task SetSelectedIndex(int index)
    {
        if (index < 0 || index > this.Data.Count - 1)
            return Task.CompletedTask;
        var item = this.Data[index];
        if (SelectedItems.Contains(item) == false)
        {
            this.SelectedItems.Add(item);
            this.NotifySelectionChanged();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Selects a single item
    /// </summary>
    /// <param name="item">the item to select</param>
    internal void SelectItem(TItem item)
    {
        this.SelectedItems.Clear();
        this.SelectedItems.Add(item);
        this.StateHasChanged();
    }

    /// <summary>
    /// Calls StateHasChanged on the component
    /// </summary>
    public void TriggerStateHasChanged() => this.StateHasChanged();

    private void CheckItem(ChangeEventArgs e, TItem item)
    {
        bool @checked = e.Value as bool? == true;
        if (@checked)
        {
            if (this.SelectedItems.Contains(item) == false)
            {
                this.SelectedItems.Add(item);
                this.NotifySelectionChanged();
            }
        }
        else if (this.SelectedItems.Contains(item))
        {
            this.SelectedItems.Remove(item);
            this.NotifySelectionChanged();
        }
    }

    async Task FilterData(bool clearSelected = true)
    {
        if (clearSelected && this.SelectedItems.Any())
        {
            this.SelectedItems.Clear();
            this.NotifySelectionChanged();
        }
        string filter = this.FilterText.ToLowerExplicit();
        var filterEvent = new FilterEventArgs
        {
            Text = filter,
            FlowTable = this,
            PageIndex = this.Pager?.PageIndex ?? -1,
            HasPager = this.PagerVisible
        };
        await this.OnFilter.InvokeAsync(filterEvent);
        if (filterEvent.Handled)
        {
            if (filterEvent.PageIndex != -1 && filterEvent.PageIndex != this.Pager.PageIndex)
                this.Pager.PageIndex = filterEvent.PageIndex;
            return;
        }

        if (string.IsNullOrWhiteSpace(filter))
            this.DisplayData = this.DataDictionary;
        else if (filter.StartsWith(CurrentFilter))
        {
            // filtering the same set
            this.DisplayData = this.DisplayData.Where(x => x.Value.Contains(filter))
                                   .ToDictionary(x => x.Key, x => x.Value);
        }
        else
        {
            this.DisplayData = this.DataDictionary.Where(x => x.Value.Contains(filter))
                                   .ToDictionary(x => x.Key, x => x.Value);
        }
        CurrentFilter = filter;
        await InvokeAsync(this.StateHasChanged);
    }

    private Task OnClick(MouseEventArgs e, TItem item)
    {
        bool changed = false;
        bool wasSelected = this.SelectedItems.Contains(item);

        if (ForcedSelection?.Contains(item) == true)
            return Task.CompletedTask;
        
        if (e.CtrlKey || e.OffsetX < 35 || DontClearOnClick) // FF-1073 - 35 makes it easier to select multiple without unselecting others
        {
            // multiselect changing one item
            if (wasSelected)
                this.SelectedItems.Remove(item);
            else
                this.SelectedItems.Add(item);
            changed = true;
        }
        else if (e.CtrlKey == false && e.ShiftKey == false)
        {
            // just select/unselect this one
            if(wasSelected && this.SelectedItems.Count == 1)
                this.SelectedItems.Clear();
            else
            {
                this.SelectedItems.Clear();
                this.SelectedItems.Add(item);
            }
            changed = true;
        }
        else
        {
            if (wasSelected == false)
            {
                this.SelectedItems.Add(item);
                changed = true;
            }

            if (this.LastSelected != null && e.ShiftKey)
            {
                // select everything in between
                int last = this.Data.IndexOf(this.LastSelected);
                int current = this.Data.IndexOf(item);
                int start = last > current ? current : last;
                int end = last > current ? last : current;

                bool unselecting = e.CtrlKey;
                for (int i = start; i <= end && i < Data.Count - 1; i++)
                {
                    var tItem = this.Data[i];
                    if (unselecting)
                    {
                        if (this.SelectedItems.Contains(tItem))
                        {
                            this.SelectedItems.Remove(tItem);
                            changed = true;
                        }
                    }
                    else if (this.SelectedItems.Contains(tItem) == false)
                    {
                        this.SelectedItems.Add(tItem);
                        changed = true;
                    }
                }
            }
        }

        if(changed)
            this.NotifySelectionChanged();

        this.LastSelected = item;
        return Task.CompletedTask;
    }

    private async Task OnDoubleClick(TItem item)
    {
        this.SelectedItems.Clear();
        this.SelectedItems.Add(item);
        this.NotifySelectionChanged();
        await this.DoubleClick.InvokeAsync(item);
    }

    private void NotifySelectionChanged()
    {
        NotifySelectionChanged(SelectedItems.Cast<object>().ToList());
    }

    private Task FilterKeyDown(KeyboardEventArgs args)
    {
        if(args.Key == "Escape")
        {
            this.FilterText = String.Empty;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Triggers the page change event
    /// </summary>
    /// <param name="page">the page to change to</param>
    public void TriggerPageChange(int page) => _ = OnPageChange.InvokeAsync(page);
    
    /// <summary>
    /// Triggers the page size change event
    /// </summary>
    /// <param name="size">the new size</param>
    public void TriggerPageSizeChange(int size) => _ = OnPageSizeChange.InvokeAsync(size);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private Task ContextButton(FlowTableButton btn)
    {
        _ = btn.OnClick();
        return Task.CompletedTask;
    }

    private void ContextMenuPreShow()
    {
        var items = this.ToolbarItems?.Select(x =>
        {
            if (x is FlowTableButtonSeparator separtor)
            {
                if (separtor.Visible == false)
                    return null;
                return new FlowContextMenuItem()
                {
                    Separator = true
                };
            }

            if (x is FlowTableButton btn)
            {
                if (btn.Enabled != true || btn.Visible != true || btn.Area == ButtonArea.Toolbar)
                    return null;
                return new()
                {
                    Label = btn.Label,
                    Icon = btn.Icon,
                    IsHelpButton = btn is FlowTableHelpButton,
                    OnClick = () => { _ = btn.OnClick(); }
                };
            }

            return null;
        }).Where(x => x != null).ToList();

        TableContextMenu.SetItems((items ?? new ())!);
    }
}
public enum SelectionMode
{
    None,
    Single,
    Multiple
}

/// <summary>
/// Arguments for filter event
/// </summary>
public class FilterEventArgs
{
    /// <summary>
    /// Gets or sets if this event has been handled
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Gets or sets the page index
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// Gets the flow table
    /// </summary>
    public FlowTableBase FlowTable { get; init; }

    /// <summary>
    /// Gets if there is a pager
    /// </summary>
    public bool HasPager { get; init; }

    /// <summary>
    /// Gets the filter text
    /// </summary>
    public string Text { get; init; }
}