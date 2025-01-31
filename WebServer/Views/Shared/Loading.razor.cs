using FileFlows.Services;
using Microsoft.AspNetCore.Components;

namespace FileFlows.WebServer.Views.Shared;

/// <summary>
/// Loading component shown while the application is starting
/// </summary>
public partial class Loading : ComponentBase
{
    /// <summary>
    /// The current status message
    /// </summary>
    private string Status = "Initializing";

    /// <summary>
    /// Additional details
    /// </summary>
    private string Details = string.Empty;

    /// <summary>
    /// The sub status text
    /// </summary>
    private string SubStatus = string.Empty;

    /// <summary>
    /// If the details are expanded or collapsed
    /// </summary>
    private bool Expanded = false;
    
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        var service = ServiceLoader.Load<IStartupService>();
        Status = service.CurrentStatus;
        service.OnStatusUpdate += (message, substatus,  details) =>
        {
            _ = InvokeAsync(() =>
            {
                if (string.IsNullOrWhiteSpace(message))
                    return;
                
                Status = message;
                SubStatus = substatus ?? string.Empty;
                Details = details ?? string.Empty;
                StateHasChanged();
            });
        };
    }

    /// <summary>
    /// Toggles the expanded state
    /// </summary>
    void ToggleExpanded()
    {
        Expanded = !Expanded;
    }
}