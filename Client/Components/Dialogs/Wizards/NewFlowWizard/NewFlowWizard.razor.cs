using FileFlows.Client.Components.Common;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Dialogs.Wizards;

/// <summary>
/// New Flow wizard
/// </summary>
public partial class NewFlowWizard : IModal
{
    
    /// <summary>
    /// Gets or sets the navigation manager used
    /// </summary>
    [Inject] private NavigationManager NavigationManager { get; set; }
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    
    private Editor _Editor;

    /// <summary>
    /// Flow values
    /// </summary>
    private int SelectedCategory = 0, FlowBasic = 0, FlowVideo = 0, FlowAudio = 0, FlowImage = 0, FlowBook = 1, FlowFailure;

    /// <summary>
    /// Gets or sets the editor
    /// </summary>
    public Editor Editor
    {
        get => _Editor;
        set
        {
            if (_Editor != value && value != null)
            {
                _Editor = value;
                StateHasChanged();
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the profile service
    /// </summary>
    [Inject] private ProfileService ProfileService { get; set; }
    
    /// <inheritdoc />
    [Parameter]
    public IModalOptions Options { get; set; }

    /// <inheritdoc />
    [Parameter]
    public TaskCompletionSource<object> TaskCompletionSource { get; set; }

    /// <summary>
    /// Gets or sets the flow wizard
    /// </summary>
    private FlowWizard Wizard { get; set; }

    // if the initialization has been done
    private bool initDone;

    /// <summary>
    /// A dictionary of all available flow elements
    /// </summary>
    private Dictionary<string, FlowElement> AvailableElements = [];

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var profile = await ProfileService.Get(); 
        var elementsResult = await HttpHelper.Get<FlowElement[]>("/api/flow/elements");
        if (elementsResult.Success)
            AvailableElements = elementsResult.Data.ToDictionary(x => x.Uid);
        
        initDone = true;
        StateHasChanged();
    }
    
    /// <summary>
    /// Closes the dialog
    /// </summary>
    public void Close()
    {
        TaskCompletionSource.TrySetCanceled(); // Set result when closing
    }

    /// <summary>
    /// Cancels the dialog
    /// </summary>
    public void Cancel()
    {
        TaskCompletionSource.TrySetCanceled(); // Indicate cancellation
    }

    /// <summary>
    /// Selects the current flow for creation
    /// </summary>
    public async Task Select()
    {
        bool close = true;
        switch (SelectedCategory)
        {
            case 0: // Basic
                CreateBasic();
                break;
            case 1: // Video
                close = await CreateVideo();
                break;
            case 2: // Audio
                close = await CreateAudio();
                break;
            case 3: // Image
                close = await CreateImage();
                break;
            case 4: // Book
                close = await CreateBook();
                break;
            case 5: // Failure
                await CreateFailure();
                break;
        }
        if(close)
            TaskCompletionSource.TrySetResult(null);
    }

    /// <summary>
    /// Creates a basic flow
    /// </summary>
    private void CreateBasic()
    {
        switch (FlowBasic)
        {
            case 0: // Blank File
                CreateBasicFlow(FlowElementUids.InputFile);
                return;
            case 1: // Blank Folder
                CreateBasicFlow(FlowElementUids.InputFolder);
                return;
            case 2: // Blank Failure Flow
                CreateBasicFlow(FlowElementUids.FlowFailure);
                return;
            case 3: // Blank Sub Flow
                CreateBasicFlow(FlowElementUids.SubFlowInput);
                return;
            case 4: // Blank Input URL
                CreateBasicFlow(FlowElementUids.InputUrl);
                return;
        }
    }
    
    
    /// <summary>
    /// Creates a failure flow
    /// </summary>
    private async Task CreateFailure()
    {
        if (FlowFailure == 0)
        {
            // Blank File
            CreateBasicFlow(FlowElementUids.FlowFailure);
            return;
        }

        string name = FlowFailure switch
        {
            1 => "Apprise Notification",
            2 => "Discord Notification",
            3 => "Gotify Notification",
            4 => "Telegram Notification",
            _ => "Failure Notification"
        };
        

        var builder = new FlowBuilder(name);
        builder.Add(new ()
        {
            FlowElementUid = FlowElementUids.FlowFailure,
            Type = FlowElementType.Input,
        });

        if (FlowFailure == 1)
        {
            builder.AddAndConnect(new()
            {
                FlowElementUid = FlowElementUids.Apprise,
                Type = FlowElementType.Communication,
                Model = ExpandoHelper.ToExpandoObject(new
                {
                    MessageType = "warning",
                    Message = "FileFlows - File \u0027{file.FullName}\u0027 failed",
                    Tag = new string[] {}
                })
            });
        }
        else if (FlowFailure == 2)
        {
            builder.AddAndConnect(new()
            {
                FlowElementUid = FlowElementUids.Discord,
                Type = FlowElementType.Communication,
                Model = ExpandoHelper.ToExpandoObject(new
                {
                    Message = "{file.FullName}",
                    Title = "FileFlows - File Failed",
                    MessageType = "Warning"
                })
            });
        }
        else if (FlowFailure == 3)
        {
            builder.AddAndConnect(new()
            {
                FlowElementUid = FlowElementUids.Gotify,
                Type = FlowElementType.Communication,
                Model = ExpandoHelper.ToExpandoObject(new
                {
                    Message = "{file.FullName}",
                    Title = "FileFlows - File Failed",
                    Priority = 2
                })
            });
        }
        else if (FlowFailure == 4)
        {
            builder.AddAndConnect(new()
            {
                FlowElementUid = FlowElementUids.Telegram,
                Type = FlowElementType.Communication,
                Model = ExpandoHelper.ToExpandoObject(new
                {
                    Message = "FileFlows - File \u0027{file.FullName}\u0027 failed"
                })
            });
        }

        var saveResult = await HttpHelper.Put<Flow>("/api/flow?uniqueName=true", builder.Flow);
        if (saveResult.Success == false)
        {
            Wizard.HideBlocker();
            Toast.ShowEditorError( Translater.TranslateIfNeeded(saveResult.Body?.EmptyAsNull() ?? "ErrorMessages.SaveFailed"));
            return;
        }
        
        NavigationManager.NavigateTo("flows/" + saveResult.Data.Uid);
    }

    /// <summary>
    /// Creates a video flow
    /// </summary>
    /// <returns>if the dialog should be closed</returns>
    private async Task<bool> CreateVideo()
    {
        switch (FlowVideo)
        {
            case 0: // Convert Video
            {
                var result = await ModalService.ShowModal<NewVideoFlowWizard, Flow>(new NewVideoFlowWizardOptions());
                if (result.Success(out var newFlow))
                {
                    NavigationManager.NavigateTo($"flows/{newFlow.Uid}");
                    return true;
                }
            }
                return false;
            case 1: // Blank Video
                CreateBasicFlow(FlowElementUids.VideoFile);
                return true;
            case 2: // Audio to Video
            {
                var result =
                    await ModalService.ShowModal<NewAudioToVideoWizard, Flow>(new NewAudioToVideoWizardOptions());
                if (result.Success(out var newFlow))
                {
                    NavigationManager.NavigateTo($"flows/{newFlow.Uid}");
                    return true;
                }
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Creates a video flow
    /// </summary>
    /// <returns>if the dialog should be closed</returns>
    private async Task<bool> CreateAudio()
    {
        switch (FlowAudio)
        {
            case 0: // Convert Audio
            {
                var result = await ModalService.ShowModal<NewAudioFlowWizard, Flow>(new NewAudioFlowWizardOptions());
                if (result.Success(out var newFlow))
                {
                    NavigationManager.NavigateTo($"flows/{newFlow.Uid}");
                    return true;
                }
            }
                return false;
            case 1: // Blank Audio
                CreateBasicFlow(FlowElementUids.AudioFile);
                return true;
            case 2: // Audio to Video
            {
                var result = await ModalService.ShowModal<NewAudioToVideoWizard, Flow>(new NewAudioToVideoWizardOptions());
                if (result.Success(out var newFlow))
                {
                    NavigationManager.NavigateTo($"flows/{newFlow.Uid}");
                    return true;
                }

                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Creates a video flow
    /// </summary>
    /// <returns>if the dialog should be closed</returns>
    private async Task<bool> CreateImage()
    {
        switch (FlowImage)
        {
            case 0: // Convert Image
            {
                var result = await ModalService.ShowModal<NewImageFlowWizard, Flow>(new NewImageFlowWizardOptions());
                if (result.Success(out var newFlow))
                {
                    NavigationManager.NavigateTo($"flows/{newFlow.Uid}");
                    return true;
                }
            }
                return false;
            case 1: // Blank Image
                CreateBasicFlow(FlowElementUids.ImageFile);
                return true;
        }
        return false;
    }

    /// <summary>
    /// Creates a video flow
    /// </summary>
    /// <returns>if the dialog should be closed</returns>
    private async Task<bool>  CreateBook()
    {
        switch (FlowBook)
        {
            case 0: // eBook
                return true;
            case 1: // Comic Book
            {
                var result = await ModalService.ShowModal<NewComicFlowWizard, Flow>(new NewComicFlowWizardOptions());
                if (result.Success(out var newFlow))
                {
                    NavigationManager.NavigateTo($"flows/{newFlow.Uid}");
                    return true;
                }
            }
                return false;
        }
        return false;
    }

    /// <summary>
    /// Creates a basic flow with a single input flow element
    /// </summary>
    /// <param name="inputFlowElementUid">the UID of the input flow element</param>
    private void CreateBasicFlow(string inputFlowElementUid)
    {
        var flow = new Flow()
        {
            Name = "New Flow",
            Uid = Guid.NewGuid()
        };
        if (inputFlowElementUid == FlowElementUids.SubFlowInput)
            flow.Type = FlowType.SubFlow;
        else if (inputFlowElementUid == FlowElementUids.FlowFailure)
            flow.Type = FlowType.Failure;
        var part =
            new FlowPart()
            {
                FlowElementUid = inputFlowElementUid,
                Uid = Guid.NewGuid(),
                Outputs = 1,
                Type = FlowElementType.Input,
                xPos = 80,
                yPos = 80
            };

        if (AvailableElements.TryGetValue(inputFlowElementUid, out var element))
        {
            part.Icon = element.Icon;
            part.Inputs = element.Inputs;
            part.Outputs = element.Outputs;
            part.Icon = element.Icon;
        }
        
        flow.Parts = [ part ];
        Pages.Flow.NewFlowToOpen = flow;
        NavigationManager.NavigateTo("flows/" + flow.Uid);
    }
}

/// <summary>
/// The New Flow Wizard Options
/// </summary>
public class NewFlowWizardOptions : IModalOptions
{
}
