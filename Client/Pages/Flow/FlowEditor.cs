using System.Collections;
using BlazorContextMenu;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Plugin;
using Microsoft.JSInterop;
using ffPart = FileFlows.Shared.Models.FlowPart;
using FFlow = FileFlows.Shared.Models.Flow;

namespace FileFlows.Client.Pages;

public class FlowEditor : IDisposable
{
    private Pages.Flow FlowPage { get; set; }
    public FFlow Flow { get; set; }
    private IJSRuntime jsRuntime => FlowPage.jsRuntime;
    private IBlazorContextMenuService ContextMenuService => FlowPage.ContextMenuService;
    
    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    private ProfileService ProfileService { get; set; }
    /// <summary>
    /// The users profile
    /// </summary>
    private Profile Profile;
    
    /// <summary>
    /// Gets or sets if this flow is dirty and has any changes
    /// </summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// The reference to the ffFlow object created in javasscript
    /// </summary>
    public ffFlowWrapper ffFlow { get; private set; }

    private string lblObsoleteMessage;
    const string API_URL = "/api/flow";
    private DateTime LoadedAt;

    public FlowEditor(Flow flowPage, FFlow flow, ProfileService profileService)
    {
        this.FlowPage = flowPage;
        this.Flow = flow;
        this.ProfileService = profileService;
    }

    /// <summary>
    /// Initializes the new flow editor instance
    /// </summary>
    public async Task Initialize()
    {
        LoadedAt = DateTime.UtcNow;
        if (Flow.Uid == Guid.Empty)
            Flow.Uid = Guid.NewGuid();
        Profile = await ProfileService.Get();
        ffFlow = await ffFlowWrapper.Create(jsRuntime, Flow.Uid, Flow.ReadOnly);
        ffFlow.OnAddElement = AddElement;
        ffFlow.OnOpenContextMenu = OpenContextMenu;
        ffFlow.OnEdit = Edit;
        ffFlow.OnMarkDirty = () =>
        {
            IsDirty = true;
            FlowPage.TriggerStateHasChanged();
        };
        ffFlow.OnCtrlDblClick = (ffpart) =>
        {
            if (ffpart.Type == FlowElementType.SubFlow && ffpart.FlowElementUid?.StartsWith("SubFlow:") == true && Guid.TryParse(ffpart.FlowElementUid[8..], out Guid subFlowUid))
                _ = FlowPage.OpenFlowInNewTab(subFlowUid, showBlocker: true);
        };
        
        lblObsoleteMessage = Translater.Instant("Labels.ObsoleteConfirm.Message");
        await ffFlow.InitModel(Flow);
        
        await ffFlow.init(Flow.Parts, FlowPage.Available);

        //await WaitForRender();
        await ffFlow.redrawLines();
    }



    public async Task<object> Edit(ffPart part, bool isNew)
    {
        var parts = await ffFlow.getModel();
        return await FlowPage.Edit(this, part, isNew, parts);
    }
    
    public async Task<object> AddElement(string uid)
    {
        var element = FlowPage.Available.FirstOrDefault(x => x.Uid == uid);
        string name;
        if (element.Type is FlowElementType.Script or FlowElementType.SubFlow)
        {
            // special type
            name = element.Name;
        }
        else if(element.Uid.StartsWith("SubFlowOutput"))
        {
            // sub flow element, keep its name
            name = element.Name;
        }
        else
        {
            string type = element.Uid[(element.Uid.LastIndexOf(".", StringComparison.Ordinal) + 1)..];
            name = Translater.Instant($"Flow.Parts.{type}.Label", suppressWarnings: true);
            if (name == "Label")
                name = FlowHelper.FormatLabel(type);
        }

        if (element.Obsolete)
        {
            string msg = element.ObsoleteMessage?.EmptyAsNull() ?? lblObsoleteMessage;
            string confirmMessage = Translater.Instant("Labels.ObsoleteConfirm.Question");
            string title = Translater.Instant("Labels.ObsoleteConfirm.Title");

            msg += "\n\n" + confirmMessage;
            var confirmed = await Confirm.Show(title, msg);
            if (confirmed == false)
                return null;
        }

        if ((int)element.LicenseLevel > (int)Profile.LicenseLevel)
        {
            await MessageBox.Show(
                Translater.Instant("Labels.Unlicensed"), 
                Translater.Instant("Labels.UnlicensedFlowElement", new {level = element.LicenseLevel.ToString()})
                );
            return null;
        }

        element.Name = name;
        return new { element, uid = Guid.NewGuid() };
    }


    public List<FlowPart> SelectedParts { get; private set; }
    public async Task OpenContextMenu(Flow.OpenContextMenuArgs args)
    {
        SelectedParts = args.Parts ?? new();
        await ContextMenuService.ShowMenu(SelectedParts.Count == 1 ? "FlowContextMenu-Single" :
            SelectedParts.Count > 1 ? "FlowContextMenu-Multiple" : "FlowContextMenu-Basic", args.X, args.Y);
    }
    
    
    private async Task Save()
    {
        // this.Blocker.Show(lblSaving);
        // this.IsSaving = true;
        try
        {
            var parts = await ffFlow.getModel();

            //Flow ??= new ff();
            //Flow.Name = this.Name;
            // ensure there are no duplicates and no rogue connections
            Guid[] nodeUids = parts.Select(x => x.Uid).ToArray();
            foreach (var p in parts)
            {
                p.OutputConnections = p.OutputConnections
                    ?.Where(x => nodeUids.Contains(x.InputNode))
                    ?.GroupBy(x => x.Output).Select(x => x.First())
                    ?.ToList();
            }
            //Model.Parts = parts;
            var result = await HttpHelper.Put<FFlow>(API_URL, Flow);
            if (result.Success)
            {
                if ((Profile.ConfigurationStatus & ConfigurationStatus.Flows) != ConfigurationStatus.Flows)
                {
                    // refresh the app configuration status
                    await ProfileService.Refresh();
                }

                Flow = result.Data;
                //IsDirty = false;
            }
            else
            {
                Toast.ShowEditorError(
                    result.Success || string.IsNullOrEmpty(result.Body) ? Translater.Instant($"ErrorMessages.UnexpectedError") : Translater.TranslateIfNeeded(result.Body),
                    duration: 60_000
                );
            }
        }
        finally
        {
            //this.IsSaving = false;
            //this.Blocker.Hide();
        }
    }

    public void SetVisibility(bool show)
        => _ = ffFlow.setVisibility(show);

    public void Dispose()
        => _ = ffFlow.dispose();

    /// <summary>
    /// Gets the complete Flow model with the elements etc
    /// </summary>
    /// <returns>the complete Flow model</returns>
    public async Task<FFlow> GetModel()
    {
        Flow.Parts  = await ffFlow.getModel();
        // ensure there are no duplicates and no rogue connections
        Guid[] nodeUids = Flow.Parts.Select(x => x.Uid).ToArray();
        foreach (var p in  Flow.Parts)
        {
            p.OutputConnections = p.OutputConnections
                ?.Where(x => nodeUids.Contains(x.InputNode))
                ?.GroupBy(x => x.Output).Select(x => x.First())
                ?.ToList();
        }
        return Flow;
    }

    /// <summary>
    /// Updates the model bound to this flow editor
    /// </summary>
    /// <param name="updatedModel">the updated model</param>
    /// <param name="clean">if the editor should be marked as clean</param>
    public void UpdateModel(FFlow updatedModel, bool clean = false)
    {
        if (clean)
            this.IsDirty = false;

        this.Flow = updatedModel;
    }

    /// <summary>
    /// Marks the editor as dirty
    /// </summary>
    public void MarkDirty()
    {
        if (LoadedAt > DateTime.UtcNow.AddSeconds(-1))
            return; // this can be triggered when some values are defaulted on bound controls, dont mark it as dirty this soon
        this.IsDirty = true;
        FlowPage.TriggerStateHasChanged();
    }
    
    
} 