using FileFlows.Client.Components.Editors;
using FileFlows.Client.Services.Frontend;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Inputs;

public abstract class ExecuteFlowElementView : Input<IEnumerable<ExecutedNode>>
{
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    
    protected string _Log;
    /// <summary>
    /// Gets or sets the log for this item
    /// </summary>
    [Parameter]
    public string Log
    {
        get => _Log; 
        set => _Log = value;
    }
    protected string lblClose, lblLogPartialNotAvailable, lblViewLog;
    
    protected bool Maximised { get; set; }
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }


    private List<string> _LogLines;
    private List<string> LogLines
    {
        get
        {
            if (_LogLines == null)
            {
                _LogLines = (Log ?? string.Empty).Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            return _LogLines;
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        this.lblClose = Translater.Instant("Labels.Close");
        this.lblLogPartialNotAvailable = Translater.Instant("Labels.LogPartialNotAvailable");
        this.lblViewLog = Translater.Instant("Labels.ViewLog");
    }
    
    protected void OpenLog(ExecutedNode node)
    {
        int index = Value.ToList().IndexOf(node);
        if (index < 0)
        {
            feService.Notifications.ShowWarning(lblLogPartialNotAvailable);
            return;
        }

        this.Maximised = false;
        ++index;
        var lines  = LogLines;
        int startIndex = lines.FindIndex(x =>
            x.IndexOf($"Executing Flow Element {index}:", StringComparison.Ordinal) > 0 ||
            x.IndexOf($"Executing Node {index}:", StringComparison.Ordinal) > 0);
        if (startIndex < 1)
        {
            feService.Notifications.ShowWarning(lblLogPartialNotAvailable);
            return;
        }

        --startIndex;

        var remainingLindex = lines.Skip(startIndex + 3).ToList();

        int endIndex = remainingLindex.FindIndex(x => x.IndexOf("======================================================================", StringComparison.Ordinal) > 0);

        string sublog;
        if (endIndex > -1)
        {
            endIndex += startIndex + 4;
            sublog = string.Join("\n", lines.ToArray()[startIndex..endIndex]);
        }
        else
        {
            sublog = string.Join("\n", lines.ToArray()[startIndex..]);
        }
        
        _ =  ModalService.ShowModal<LogViewer>(new ModalEditorOptions()
        {
            Model = sublog
        });
    }
    
    
    protected void OnMaximised(bool maximised)
    {
        this.Maximised = maximised;
    }
    
    
    // Unicode character for vertical line
    const char verticalLine = '\u2514'; 
    // Unicode character for horizontal line
    const char horizontalLine = '\u2500'; 
    
    protected string FormatNodeName(ExecutedNode node)
    {
        string prefix = GetFlowPartPrefix(node.Depth);
        
        if (string.IsNullOrEmpty(node.NodeName))
            return prefix + FormatNodeUid(node.NodeUid);
        
        return prefix + node.NodeName;
    }
    
    
    /// <summary>
    /// Gets the prefix to show in front of the executed flow element
    /// </summary>
    /// <param name="depth">the depth the flow element was executed in</param>
    /// <returns>the prefix to show</returns>
    private string GetFlowPartPrefix(int depth)
    {
        if (depth < 1)
            return string.Empty;
        var prefix = string.Empty + verticalLine + horizontalLine;
        for (int i = 1; i < depth; i++)
            prefix += horizontalLine.ToString() + horizontalLine;
        
        return prefix + " ";

    }
    protected string FormatNodeUid(string name)
    {
        //FlowElement.FormatName(name);
        return name[(name.LastIndexOf('.') + 1)..].Humanize(LetterCasing.Title)
            .Replace("File Flows", "FileFlows")
            .Replace("MKV", "MKV")
            .Replace("Mp4", "MP4")
            .Replace("MP 4", "MP4")
            .Replace("Ffmpeg Builder", "FFMPEG Builder:")
            .Replace("Audio Track Remover", "Track Remover"); // FF-235
    }
    
}