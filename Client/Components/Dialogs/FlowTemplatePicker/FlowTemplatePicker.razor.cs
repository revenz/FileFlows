
using BlazorMonaco;
using FileFlows.Client.Components.Dialogs.Wizards;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace FileFlows.Client.Components.Dialogs;

/// <summary>
/// A modal dialog for selecting a flow template
/// </summary>
public partial class FlowTemplatePicker : VisibleEscapableComponent
{
    /// <summary>
    /// Gets or sets the blocker to use
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    private string lblTitle, lblFilter, lblNext, lblCancel, lblOpen;
    private string lblMissingDependencies, lblMissingDependenciesMessage;
    private string FilterText = string.Empty;
    TaskCompletionSource<FlowTemplatePickerResult> ShowTask;

    private FlowTemplateModel Selected = null;
    private string SelectedTag = string.Empty;
    private string SelectedSubTag = string.Empty;
    private List<FlowTemplateModel> FilteredTemplates;
    
    /// <summary>
    /// Gets or sets the available templates
    /// </summary>
    private List<FlowTemplateModel> Templates { get; set; }

    private List<string> Tags { get; set; }


    protected override void OnInitialized()
    {
        lblTitle = Translater.Instant("Dialogs.FlowTemplatePicker.Title");
        lblNext = Translater.Instant("Labels.Next");
        lblCancel = Translater.Instant("Labels.Cancel");
        lblFilter = Translater.Instant("Labels.Filter");
        lblOpen = Translater.Instant("Labels.Open");
        lblMissingDependencies = Translater.Instant("Labels.MissingDependencies");
        lblMissingDependenciesMessage = Translater.Instant("Labels.MissingDependenciesMessage");
    }

    public Task<FlowTemplatePickerResult> Show(FlowType type)
    {
        Selected = null;
        FilterText = string.Empty;
        SelectedTag = string.Empty;
        SelectedSubTag = string.Empty;
        Visible = true;
        ShowTask = new TaskCompletionSource<FlowTemplatePickerResult?>()!;
        Blocker.Show();
        Task.Run(async () =>
        {
            Templates = await GetTemplates(type);
            Filter();
            Tags = Templates.SelectMany(x => x.Tags).Distinct().OrderBy(x => x == "Basic" ? 1 : 2).ThenBy(x => x).ToList();
            if(Tags.FirstOrDefault() == "Basic")
                SelectedTag = Tags[0];
            Blocker.Hide();
            StateHasChanged();
        });
        return ShowTask.Task;
    }

    void SelectTemplate(FlowTemplateModel item, bool andAccept = false)
    {
        if (item.MissingDependencies?.Any() == true)
        {
            if (andAccept)
            {
                _ = MessageBox.Show(lblMissingDependencies,
                    lblMissingDependenciesMessage.Replace("#LIST#", string.Join(string.Empty,
                        item.MissingDependencies.Select(x => "- " + x + "\n"))));
            }

            return;
        }
        
        Selected = item;
        if (andAccept)
            _ = New();
        StateHasChanged();
    }

    /// <summary>
    /// Opens a local flow
    /// </summary>
    Task Open()
    {
        string sUid = Selected.Path[6..];
        if (Guid.TryParse(sUid, out Guid uid) == false)
            return Task.CompletedTask;
        
        ShowTask.SetResult(new()
        {
            Result = FlowTemplatePickerResult.ResultCode.Open,
            Uid = uid
        });
        Visible = false;
        return Task.CompletedTask;
    }

    async Task New()
    {
        if (Selected?.Path == "wizard:video")
        {
            Visible = false;
            var result = await ModalService.ShowModal<NewVideoFlowWizard, Flow>(new NewVideoFlowWizardOptions());
            if (result.Success(out var newFlow))
            {
                ShowTask.SetResult(new()
                {
                    Result = FlowTemplatePickerResult.ResultCode.Open,
                    Uid = newFlow.Uid
                });
            }
            return;
        }
        
        Blocker.Show();
        
        StateHasChanged();
        try
        {
            var flowResult =
                await HttpHelper.Post<FlowTemplateModel>("/api/flow-template", Selected);
            if (flowResult.Success == false)
            {
                return;
            }

            flowResult.Data.Flow.Uid = Guid.NewGuid(); // ensure its a new UID and not an existing one
            
            // this gets the translated name, if it was translated
            flowResult.Data.Flow.Name = flowResult.Data.Name;
            
            ShowTask.SetResult(new()
            {
                Result = FlowTemplatePickerResult.ResultCode.Template,
                Model = flowResult.Data
            });
            Visible = false;
            
        }
        finally
        {
            Blocker.Hide();
            StateHasChanged();
        }
        
    }

    public override void Cancel()
    {
        ShowTask.SetResult(new FlowTemplatePickerResult { Result = FlowTemplatePickerResult.ResultCode.Cancel });
        this.Visible = false;
    }

    void ToggleTag(MouseEventArgs ev, string tag)
    {
        if (SelectedTag == tag)
        {
            SelectedTag = string.Empty;
            SelectedSubTag = string.Empty;
        }
        else
        {
            SelectedTag = tag;
            SelectedSubTag = string.Empty;
        }

        if (Selected != null && Selected.Tags.Contains(SelectedTag) == false)
            Selected = null; // clear it
    }

    void ToggleSubTag(MouseEventArgs ev, string tag)
    {
        if (SelectedSubTag == tag)
            SelectedSubTag = string.Empty;
        else
            SelectedSubTag = tag;

        if (Selected != null && Selected.Tags.Contains(SelectedSubTag) == false)
            Selected = null; // clear it
    }

    private void FilterKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            FilterText = string.Empty;
            Filter();
        }
        else if (args.Key == "Enter")
            Filter();
    }

    void Filter()
    {
        if (string.IsNullOrWhiteSpace(FilterText))
        {
            FilteredTemplates = Templates;
            return;
        }

        string text = FilterText.ToLowerInvariant();
        FilteredTemplates = Templates.Where(x =>
                x.Name?.ToLowerInvariant().Contains(text) == true || x.Description?.ToLowerInvariant().Contains(text) == true || x.Author?.ToLowerInvariant().Contains(text) == true)
            .ToList();
        // clear the selected tag
        SelectedTag = string.Empty;
    }
    
    /// <summary>
    /// Shows the new flow editor
    /// </summary>
    public async Task<List<FlowTemplateModel>> GetTemplates(FlowType type)
    {
        var result = await HttpHelper.Get<List<FlowTemplateModel>>("/api/flow-template?type=" + type);
        if (!result.Success)
            return new ();
        var results =  result.Data ?? new();

        foreach (var template in results)
        {
            string key = "Templates.Flows." + template.Name.Replace(" ", "") + ".";

            string translatedName = Translater.Instant(key + "Name", suppressWarnings: true);
            if (string.IsNullOrWhiteSpace(translatedName) == false && translatedName != "Name")
                template.Name = translatedName;
            string translatedDescription = Translater.Instant(key + "Description", suppressWarnings: true);
            if (string.IsNullOrWhiteSpace(translatedDescription) == false && translatedDescription != "Description")
                template.Description = translatedDescription;
        }

        if (type == FlowType.Standard || (int)type == -1)
        {
            results.Add(new ()
            {
                Author = "FileFlows",
                Path = "wizard:video",
                Name = "Video Wizard",
                Description = "A guided wizard to creating a video conversion flow",
                Plugins = ["Video Nodes", "Meta Nodes"],
                Tags = ["Basic", "Video", "Wizard"],
            });
        }

        return results;

    }
}
/// <summary>
/// Represents the result of a flow template picker operation.
/// </summary>
public class FlowTemplatePickerResult
{
    /// <summary>
    /// Enumeration representing the result codes.
    /// </summary>
    public enum ResultCode
    {
        /// <summary>
        /// Indicates that the operation was cancelled.
        /// </summary>
        Cancel = 0,

        /// <summary>
        /// Indicates that a template was selected.
        /// </summary>
        Template = 1,

        /// <summary>
        /// Indicates that a flow should be opened.
        /// </summary>
        Open = 2
    }

    /// <summary>
    /// Gets or sets the result code.
    /// </summary>
    public ResultCode Result { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid? Uid { get; set; }

    /// <summary>
    /// Gets or sets the flow template model.
    /// </summary>
    public FlowTemplateModel Model { get; set; }
}
