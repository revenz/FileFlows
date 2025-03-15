using FileFlows.Client.Components.Common;

namespace FileFlows.Client.Pages;

public partial class LibraryFiles
{
    
    /// <summary>
    /// Changes to a specific page
    /// </summary>
    /// <param name="index">the page to change to</param>
    private async Task PageChange(int index)
    {
        PageIndex = index;
        await this.Refresh();
    }

    /// <summary>
    /// Updates the number of items shown on a page
    /// </summary>
    /// <param name="size">the number of items</param>
    private async Task PageSizeChange(int size)
    {
        this.PageIndex = 0;
        await this.Refresh();
    }

    private async Task OnFilter(FilterEventArgs args)
    {
        if (this.filter?.EmptyAsNull() == args.Text?.EmptyAsNull())
        {
            this.filter = string.Empty;
            this.filterStatus = null;
            return;
        }

        // int totalItems = Skybox.SelectedItem.Count;
        // if (totalItems <= args.PageSize)
        await Task.CompletedTask;
            return;
        // this.filterStatus = this.SelectedStatus;
        // // need to filter on the server side
        // args.Handled = true;
        // args.PageIndex = 0;
        // this.PageIndex = 0;
        // this.filter = args.Text;
        // await this.Refresh();
        // this.filter = args.Text; // ensures refresh didnt change the filter
    }


    /// <summary>
    /// Sets the selected node
    /// </summary>
    /// <param name="node">the node</param>
    private void SelectNode(object? node)
    {
        if (node is string str)
        {
            var n = Nodes?.Values?.FirstOrDefault(x => x.Name?.ToLowerInvariant() == str.ToLowerInvariant());
            if (n == null)
                return;
            SelectedNode = n.Uid;
        }
        else
            SelectedNode = node as Guid?;
        _ = Refresh();
    }

    /// <summary>
    /// Sets the sort by
    /// </summary>
    /// <param name="sortBy">the sort by</param>
    private void SelectSortBy(object? sortBy)
    {
        SelectedSortBy = sortBy as FilesSortBy?;
        _ = Refresh();
    }
    
    /// <summary>
    /// Sets the selected library
    /// </summary>
    /// <param name="library">the library</param>
    private void SelectLibrary(object? library)
    {
        if (library is string str)
        {
            var l = Libraries?.FirstOrDefault(x => (x.Value?.ToLowerInvariant()).Equals(str, StringComparison.InvariantCultureIgnoreCase));
            if (l == null)
                return;
            SelectedLibrary = l.Value.Key;
        }
        else
            SelectedLibrary = library as Guid?;
        _ = Refresh();
    }

    private void SelectTag(object? tag)
    {
        if (tag is string str)
        {
            var t = optionsTags?.FirstOrDefault(x => (x.Label?.ToLowerInvariant()).Equals(str, StringComparison.InvariantCultureIgnoreCase));
            if (t == null)
                return;
            SelectedTag = t.Value as Guid?;
        }
        else
            SelectedTag = tag as Guid?;
        _ = Refresh();
    }

    /// <summary>
    /// Sets the selected flow
    /// </summary>
    /// <param name="flow">the flow</param>
    private void SelectFlow(object? flow)
    {
        if (flow is string str)
        {
            var f = Flows?.FirstOrDefault(x => x.Value?.ToLowerInvariant() == str.ToLowerInvariant());
            if (f == null)
                return;
            SelectedFlow = f.Value.Key;
        }
        else
            SelectedFlow = flow as Guid?;
        _ = Refresh();
    }
}