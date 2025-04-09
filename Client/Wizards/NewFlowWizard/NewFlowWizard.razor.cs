using FileFlows.Client.Components;
using FileFlows.Client.Components.Common;
using FileFlows.Client.Services.Frontend;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Wizards;

/// <summary>
/// New Flow wizard
/// </summary>
public partial class NewFlowWizard : IModal
{
    /// <summary>
    /// Gets or sets the frontend service
    /// </summary>
    [Inject] private FrontendService feService { get; set; }
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
    private bool ShowCreated;
    /// <summary>
    /// Gets or sets if the flow shouldn't be auto navigated to
    /// </summary>
    public bool DontAutoNavigateTo { get; set; }
    private Guid FlowUid = Guid.Empty;
    private List<Flow> Flows = new List<Flow>();
    /// <summary>
    /// If the user is adding a file drop  flow
    /// </summary>
    private bool FileDropFlow;

    /// <summary>
    /// A dictionary of all available flow elements
    /// </summary>
    private Dictionary<string, FlowElement> AvailableElements = [];

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        AvailableElements =  feService.Flow.FlowElements.ToDictionary(x => x.Uid);

        if (Options is NewFlowWizardOptions options)
        {
            DontAutoNavigateTo = options.DontAutoNavigateTo;
            if (options.ShowCreated)
            {
                ShowCreated = true;
                var flowResult = await HttpHelper.Get<List<Flow>>("/api/flow/list-all");
                if (flowResult.Success)
                {
                    Flows = flowResult.Data ?? [];
                    FlowUid = Flows.FirstOrDefault()?.Uid ?? Guid.Empty;
                }
            }

            FileDropFlow = options.FileDropFlow;
        }
        
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
        Flow? flow = null;
        switch (SelectedCategory)
        {
            case 0: // Basic
                flow = CreateBasic();
                break;
            case 1: // Video
                flow = await CreateVideo();
                break;
            case 2: // Audio
                flow = await CreateAudio();
                break;
            case 3: // Image
                flow = await CreateImage();
                break;
            case 4: // Book
                flow = await CreateBook();
                break;
            case 5: // Failure
                flow =await CreateFailure();
                break;
            case 6: // Users
                flow = await GetUserFlow();
                break;
        }

        if (flow != null)
        {
            if(DontAutoNavigateTo == false)
                NavigationManager.NavigateTo("flows/" + flow.Uid);
            
            TaskCompletionSource.TrySetResult(flow);
        }
    }

    /// <summary>
    /// Gets the users flow
    /// </summary>
    /// <returns>the users flow</returns>
    private async Task<Flow?> GetUserFlow()
    {
        var flowResult = await HttpHelper.Get<Flow>($"/api/flow/{FlowUid}");
        if(flowResult.Success)
            return flowResult.Data;
        return null;
    }

    /// <summary>
    /// Creates a basic flow
    /// </summary>
    private Flow? CreateBasic()
    {
        switch (FlowBasic)
        {
            case 0: // Blank File
                return CreateBasicFlow(FlowElementUids.InputFile, "fas fa-file", "Custom file based flow.");
            case 1: // Blank Folder
                return CreateBasicFlow(FlowElementUids.InputFolder, "fas fa-folder", "Custom folder based flow.");
            case 2: // Blank Failure Flow
                return CreateBasicFlow(FlowElementUids.FlowFailure, "fas fa-exclamation-circle", "Custom failure flow.");
            case 3: // Blank Sub Flow
                return CreateBasicFlow(FlowElementUids.SubFlowInput, "fas fa-subway", "Custom sub flow.");
            case 4: // Blank Input URL
                return CreateBasicFlow(FlowElementUids.InputUrl, "fas fa-globe", "Custom URL processing flow.");
        }

        return null;
    }
    
    
    /// <summary>
    /// Creates a failure flow
    /// </summary>
    /// <returns>if the flow that was created</returns>
    private async Task<Flow?> CreateFailure()
    {
        if (FlowFailure == 0)
        {
            // Blank File
            return CreateBasicFlow(FlowElementUids.FlowFailure, "fas fa-exclamation-circle", "Custom failure flow.");
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

        var flow = builder.Flow;
        flow.Icon = FlowFailure switch
        {
            1 => "fas fa-rocket",
            2 => "fab fa-discord",
            3 => "fas fa-flag",
            4 => "fab fa-telegram-plane",
            _ => "fas fa-exclamation-circle"
        };
        flow.Description = $"Sends a {name.ToLowerInvariant()} when a failure occurs.";

        var saveResult = await HttpHelper.Put<Flow>("/api/flow?uniqueName=true", builder.Flow);
        if (saveResult.Success == false)
        {
            Wizard.HideBlocker();
            feService.Notifications.ShowEditorError( Translater.TranslateIfNeeded(saveResult.Body?.EmptyAsNull() ?? "ErrorMessages.SaveFailed"));
            return null;
        }
        
        return saveResult.Data;
    }

    /// <summary>
    /// Creates a video flow
    /// </summary>
    /// <returns>if the flow that was created</returns>
    private async Task<Flow?> CreateVideo()
    {
        switch (FlowVideo)
        {
            case 0: // Convert Video
            {
                var result = await ModalService.ShowModal<NewVideoFlowWizard, Flow>(new NewVideoFlowWizardOptions() {
                    FileDropFlow = FileDropFlow
                });
                if (result.Success(out var newFlow))
                    return newFlow;
            }
                return null;
            case 1: // Blank Video
                return CreateBasicFlow(FlowElementUids.VideoFile, "fas fa-video", 
                    "Custom video processing flow.", extensions: FlowWizardBase.Extensions_Video, FileDropPreviewMode.Thumbnails);
            case 2: // Audio to Video
            {
                var result =
                    await ModalService.ShowModal<NewAudioToVideoWizard, Flow>(new NewAudioToVideoWizardOptions() {
                        FileDropFlow = FileDropFlow
                    });
                if (result.Success(out var newFlow))
                    return newFlow;
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a video flow
    /// </summary>
    /// <returns>if the flow that was created</returns>
    private async Task<Flow?> CreateAudio()
    {
        switch (FlowAudio)
        {
            case 0: // Convert Audio
            {
                var result = await ModalService.ShowModal<NewAudioFlowWizard, Flow>(new NewAudioFlowWizardOptions() {
                    FileDropFlow = FileDropFlow
                });
                if (result.Success(out var newFlow))
                    return newFlow;
                }
                return null;
            case 1: // Blank Audio
                return CreateBasicFlow(FlowElementUids.AudioFile, "fas fa-headphones", "Custom audio processing flow.",
                    extensions: FlowWizardBase.Extensions_Audio);
            case 2: // Audio to Video
            {
                var result = await ModalService.ShowModal<NewAudioToVideoWizard, Flow>(new NewAudioToVideoWizardOptions() {
                    FileDropFlow = FileDropFlow
                });
                if (result.Success(out var newFlow))
                    return newFlow;
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Creates a video flow
    /// </summary>
    /// <returns>if the flow that was created</returns>
    private async Task<Flow?> CreateImage()
    {
        switch (FlowImage)
        {
            case 0: // Convert Image
            {
                var result = await ModalService.ShowModal<NewImageFlowWizard, Flow>(new NewImageFlowWizardOptions()
                {
                    FileDropFlow = FileDropFlow
                });
                if (result.Success(out var newFlow))
                    return newFlow;
            }
                return null;
            case 1: // Blank Image
                return CreateBasicFlow(FlowElementUids.ImageFile, "fas fa-image", "Custom image processing flow.",
                    extensions: FlowWizardBase.Extensions_Image, previewMode: FileDropPreviewMode.Images);
        }
        return null;
    }

    /// <summary>
    /// Creates a video flow
    /// </summary>
    /// <returns>if the flow that was created</returns>
    private async Task<Flow?>  CreateBook()
    {
        switch (FlowBook)
        {
            case 0: // eBook
                return null;
            case 1: // Comic Book
            {
                var result = await ModalService.ShowModal<NewComicFlowWizard, Flow>(new NewComicFlowWizardOptions()
                {
                    FileDropFlow = FileDropFlow
                });
                if (result.Success(out var newFlow))
                    return newFlow;
            }
                return null;
        }
        return null;
    }

    /// <summary>
    /// Creates a basic flow with a single input flow element
    /// </summary>
    /// <param name="inputFlowElementUid">the UID of the input flow element</param>
    /// <param name="icon">the icon for the new flow</param>
    /// <param name="description">the description for the flow</param>
    /// <param name="extensions">the extensions to accept</param>
    /// <param name="previewMode">the FileDrop preview mode</param>
    private Flow CreateBasicFlow(string inputFlowElementUid, string icon, string description, 
        string[]? extensions = null, FileDropPreviewMode previewMode = FileDropPreviewMode.List)
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
        else if (FileDropFlow)
        {
            flow.Type = FlowType.FileDrop;
            if (extensions != null)
            {
                flow.FileDropOptions = new()
                {
                    Extensions = extensions,
                    PreviewMode = previewMode
                };
            }
        }

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
        flow.Description = description;
        flow.Icon = icon;
        Pages.Flow.NewFlowToOpen = flow;
        return flow;
    }

    /// <summary>
    /// Gets the icon to show for a flow
    /// </summary>
    /// <param name="flow">the flow</param>
    /// <returns>the icon</returns>
    private string GetFlowIcon(Flow flow)
    {
        if(string.IsNullOrEmpty(flow.Icon) == false)
            return flow.Icon;
        
        var input = flow.Parts?.FirstOrDefault()?.FlowElementUid;
        if (input == FlowElementUids.FlowFailure || flow.Name.Contains("fail", StringComparison.CurrentCultureIgnoreCase))
            return "fas fa-exclamation-circle";
        if(input == FlowElementUids.SubFlowInput || flow.Name.Replace(" ", "").Contains("subflow", StringComparison.CurrentCultureIgnoreCase))
            return "fas fa-subway";
        if(input == FlowElementUids.VideoFile || 
           flow.Name.Contains("video", StringComparison.CurrentCultureIgnoreCase) ||
           flow.Name.Contains("movie", StringComparison.CurrentCultureIgnoreCase) || 
           flow.Name.Contains("tv", StringComparison.CurrentCultureIgnoreCase))
            return "fas fa-video";
        if(input == FlowElementUids.InputFile)
            return "fas fa-file";
        if(input == FlowElementUids.InputFolder)
            return "fas fa-folder";
        if(input == FlowElementUids.InputUrl|| flow.Name.Contains("web", StringComparison.CurrentCultureIgnoreCase))
            return "fas fa-globe";
        if(input == FlowElementUids.AudioFile|| flow.Name.Contains("audio", StringComparison.CurrentCultureIgnoreCase)
                                             || flow.Name.Contains("music", StringComparison.CurrentCultureIgnoreCase))
            return "fas fa-headphones";
        if(input == FlowElementUids.ImageFile|| flow.Name.Contains("image", StringComparison.CurrentCultureIgnoreCase)
                                             || flow.Name.Contains("picture", StringComparison.CurrentCultureIgnoreCase))
            return "fas fa-image";
        return "fas fa-sitemap";
    }
}

/// <summary>
/// The New Flow Wizard Options
/// </summary>
public class NewFlowWizardOptions : IModalOptions
{
    /// <summary>
    /// Gets or sets if the users created flows should be shown
    /// </summary>
    public bool ShowCreated { get; set; }
    
    /// <summary>
    /// Gets or sets if the flow shouldn't be auto navigated to
    /// </summary>
    public bool DontAutoNavigateTo { get; set; }
    
    /// <summary>
    /// Gets or sets if the user is adding a file drop flow
    /// </summary>
    public bool FileDropFlow { get; set; }
}
