using System.Text.Json;
using FileFlows.Client.Pages;
using FileFlows.Client.Services.Frontend;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Flow = FileFlows.Shared.Models.Flow;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// Executed Flow Elements Renderer Viewer
/// </summary>
public partial class InputExecutedFlowElementsRenderer : ExecuteFlowElementView, IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the model
    /// </summary>
    public List<ExecutedNode> ExecutedNodes { get; set; }
    
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }
    
    private List<FlowElement> FlowElements = [];
    private List<FlowPart> parts = [];
    private bool _needsRendering = false;

    private ffFlowWrapper ffFlow;
    private IJSObjectReference? jsObjectReference;
    private Flow? Flow;


    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        FlowElements = feService.Flow.FlowElements;
        ExecutedNodes = Value.ToList();
        
        _ = Initialize();
    }

    private async Task Initialize()
    {
        var dotNetObjRef = DotNetObjectReference.Create(this);
        var js = await jsRuntime.InvokeAsync<IJSObjectReference>("import",
            $"./Components/Inputs/InputExecutedFlowElementsRenderer/InputExecutedFlowElementsRenderer.razor.js?v={Globals.Version}");
        jsObjectReference = await js.InvokeAsync<IJSObjectReference>("createExecutedFlowElementsRenderer", dotNetObjRef, this.Uid);
        
        int height = await jsObjectReference.InvokeAsync<int>("getVisibleHeight") - 400;
        if (height < 200)
            height = 780;
        //ready = true;
        Flow = BuildFlow(height);
        ffFlow = await ffFlowWrapper.Create(jsRuntime, Guid.NewGuid(), true);
        await ffFlow.InitModel(Flow);
        await ffFlow.init(parts, FlowElements.ToArray());

        //Flow.Parts, FlowPage.Available);
        await WaitForRender();
        await ffFlow.redrawLines();
        await jsObjectReference.InvokeVoidAsync("captureDoubleClicks");
        StateHasChanged();
    }
    
    /// <summary>
    /// Waits for the component to render
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
    
    private Dictionary<Guid, ExecutedNode> ExecutedNodeDictionary = new();

    /// <summary>
    /// Builds the flow
    /// </summary>
    /// <param name="height">the maximum height of the flow</param>
    /// <returns>the built flow</returns>
    private Flow BuildFlow(int height)
    {
        int xPos = 50;
        int yPos = 20;
        Guid nextUid = Guid.NewGuid();
        for(int i=0;i<ExecutedNodes.Count;i++ )
        {
            var node = ExecutedNodes[i];
            ExecutedNodeDictionary[nextUid] = node;
            var element = FlowElements.FirstOrDefault(x => x.Uid == node.NodeUid);
            var part = new FlowPart
            {
                Uid = nextUid,
                Name = node.NodeName,
                ReadOnly = true,
                Inputs = i > 0 ? 1 : 0,
                Outputs = element?.Outputs ?? (node.NodeUid?.Contains(".Startup") == true ? 1 : 0),
                Type = element?.Type ?? FlowElementType.Input,
                FlowElementUid = node.NodeUid,
                Label = node.NodeName,
                xPos = xPos,
                yPos = yPos
            };
            SetIcon(part, node, element);
            if(part.Outputs < node.Output)
                part.Outputs = node.Output;
            
            nextUid = Guid.NewGuid();
            if(i < ExecutedNodes.Count - 1)
            {
                part.OutputConnections =
                [
                    new ()
                    {
                        Input = 1,
                        Output = node.Output,
                        InputNode = nextUid
                    }
                ];
            }
            parts.Add(part);
            yPos += 120;
            if (yPos > height)
            {
                yPos = 20;
                xPos += 250;
            }
        }
        Flow flow = new();
        flow.Parts = parts;
        return flow;
    }

    /// <summary>
    /// Sets the icon for the element
    /// </summary>
    /// <param name="element">the element</param>
    private void SetIcon(FlowPart part, ExecutedNode node, FlowElement? element)
    {
        if (string.IsNullOrWhiteSpace(element?.Icon) == false)
        {
            part.Icon = element.Icon;
            if (string.IsNullOrWhiteSpace(element.CustomColor) == false)
                part.CustomColor = element.CustomColor;
        }
        else if (node.NodeUid?.Contains(".Startup") == true)
        {
            part.Icon = "fas fa-sitemap";
            part.CustomColor = "#a428a7";
        }
        else if (node.NodeUid?.Contains(".RunnerFlowElements.FileDownloader") == true)
        {
            part.Icon = "fas fa-download";
            part.CustomColor = "#a428a7";
        }
    }

    /// <summary>
    /// A flow element was double clicked
    /// </summary>
    /// <param name="uid">the UID of the flow element</param>
    [JSInvokable]
    public void OnDoubleClick(Guid uid)
    {
        var executed = ExecutedNodeDictionary.GetValueOrDefault(uid);
        if (executed == null)
            return;
        
        OpenLog(executed);
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (jsObjectReference != null)
        {
            await ffFlow.dispose();
            await jsObjectReference.InvokeVoidAsync("dispose");
            await jsObjectReference.DisposeAsync();
        }
    }
}