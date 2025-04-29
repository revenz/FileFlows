using System.ComponentModel;
using FileFlows.Client.Services.Frontend;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Common;

public partial class FlowPager<TItem> where TItem : notnull
{
    [CascadingParameter] private FlowTable<TItem> Table { get; set; }

    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }
    /// <summary>
    /// Gets the total items in the datalist
    /// </summary>
    private int TotalItems => Table.TotalItems;

    /// <summary>
    /// Gets or sets the current page index
    /// </summary>
    public int PageIndex { get; set; }
    private int PageCount 
    {
        get
        {
            if (TotalItems == 0) return 0;
            if (TotalItems <= feService.PageSize) return 1;
            int pages = TotalItems / feService.PageSize;
            if (TotalItems % feService.PageSize > 0)
                ++pages;
            return pages;
        }
    }
    private Task PageChange(int index)
    {
        PageIndex = index;
        Table.TriggerPageChange(index);
        return Task.CompletedTask;
    }
    protected override void OnInitialized()
    {
        Table.PropertyChanged += TableOnPropertyChanged;
        Table.Pager = this;
    }

    private void TableOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName == nameof(Table.TotalItems))
            this.StateHasChanged();
    }
}