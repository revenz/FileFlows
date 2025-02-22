using System.Collections.Generic;
using System.Threading.Tasks;
using FileFlows.Client.Components.Dialogs;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

/// <summary>
/// File INput
/// </summary>
public partial class InputFile : Input<string>
{
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
 
    /// <summary>
    /// Gets or sets the allowed extensions
    /// </summary>
    [Parameter]
    public string[] Extensions { get; set; }

    /// <summary>
    /// Gets or sets if the user should pick a folder instead of file
    /// </summary>
    [Parameter]
    public bool Directory { get; set; }

    /// <summary>
    /// Gets or sets the variables
    /// </summary>
    [Parameter] public Dictionary<string, object> Variables { get; set; } = new();
    
    /// <inheritdoc />
    public override bool Focus() => FocusUid();
    
    /// <summary>
    /// Browse button clicked
    /// </summary>
    async Task Browse()
    {
        var start = Value;
        if (Directory == false)
        {
            var index = start.Replace("\\", "/").LastIndexOf('/');
            if (index > 0)
                start = start[..index];
        }
        Result<string> result = await ModalService.ShowModal<FileBrowser, string>(new FileBrowserOptions()
        {
            Directory = Directory, 
            Start = start,
            Extensions = Extensions
        });
        if (result.Failed(out _))
            return;
        var path = result.Value;
        if (string.IsNullOrWhiteSpace(path))
            return;
        
        //string result = await FileBrowser.Show(this.Value, directory: Directory, extensions: Extensions);
        // if (string.IsNullOrEmpty(result))
        //     return;
        this.ClearError();
        this.Value = path;
        StateHasChanged();
    }
}
