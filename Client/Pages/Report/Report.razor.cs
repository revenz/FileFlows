using System.Text.Json;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Pages;

/// <summary>
/// Report page
/// </summary>
public partial class Report : ComponentBase
{
    /// <summary>
    /// Gets or sets the UID of the report to run
    /// </summary>
    [Parameter] public Guid Uid { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation manager
    /// </summary>
    [Inject] public NavigationManager NavigationManager { get; set; }
    
    /// <summary>
    /// Gets or sets the client service
    /// </summary>
    [Inject] public ClientService ClientService { get; set; }
    
    /// <summary>
    /// Gets or sets the blocker
    /// </summary>
    [CascadingParameter] public Blocker Blocker { get; set; }
    /// <summary>
    /// Gets or sets the JS Runtime
    /// </summary>
    [Inject] public IJSRuntime jsRuntime { get; set; }
    
    /// <summary>
    /// Gets or sets the report instance
    /// </summary>
    private InlineEditor Editor { get; set; }
    
    /// <summary>
    /// Gets or sets the element fields
    /// </summary>
    public List<IFlowField> Fields { get; set; }
    
    /// <summary>
    /// Reference to JS Report class
    /// </summary>
    private IJSObjectReference jsReports;

    /// <summary>
    /// The model
    /// </summary>
    private ExpandoObject Model;
    /// <summary>
    /// Gets or sets the report name
    /// </summary>
    private string ReportName { get; set; }
    
    /// <summary>
    /// Gets or sets the report description
    /// </summary>
    private string ReportDescription { get; set; }
    
    /// <summary>
    /// Gets or sets the icon for the report
    /// </summary>
    public string ReportIcon { get; set; }

    /// <summary>
    /// The buttons for the form
    /// </summary>
    private List<ActionButton> Buttons = new();

    /// <summary>
    /// Gets or sets if the form is loaded
    /// </summary>
    private bool Loaded = false;
    
    /// <summary>
    /// Gets or sets the HTML of the generated report
    /// </summary>
    private string Html { get; set; }
    
    /// <summary>
    /// Gets or sets if the report output should be shown
    /// </summary>
    private bool ShowReportOutput { get; set; }
    /// <summary>
    /// Indicates if this component needs rendering
    /// </summary>
    private bool _needsRendering = false;

    /// <summary>
    /// The labels for buttons
    /// </summary>
    private string lblBack = null!, lblClose = null!, lblHelp = null!;
    
    /// <summary>
    /// Gets or sets the URL to the help page for this report
    /// </summary>
    private string HelpUrl { get; set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        lblBack = Translater.Instant("Pages.Report.Buttons.Back");
        lblClose = Translater.Instant("Labels.Close");
        lblHelp = Translater.Instant("Labels.Help");
        var jsObjectReference = await jsRuntime.InvokeAsync<IJSObjectReference>("import",
            $"./Pages/Report/Report.razor.js?v={Globals.Version}");
        jsReports = await jsObjectReference.InvokeAsync<IJSObjectReference>("createReporting",
            [DotNetObjectReference.Create(this)]);

        var result = await HttpHelper.Get<ReportDefinition>($"/api/report/definition/{Uid}");
        if (result.Success == false)
        {
            Toast.ShowError(Translater.TranslateIfNeeded(result.Body?.EmptyAsNull() ??
                                                         "Pages.Report.Messages.FailedToFindReport"));
            NavigationManager.NavigateTo("/reporting");
            return;
        }

        var rd = result.Data;
        ReportName = Translater.Instant($"Reports.{rd.Type}.Name");
        ReportDescription = Translater.Instant($"Reports.{rd.Type}.Description");;
        ReportIcon = rd.Icon;
        HelpUrl = $"https://fileflows.com/docs/webconsole/admin/reporting/{rd.Type.Kebaberize()}";

        // clone the fields as they get wiped
        var fields = new List<IFlowField>();
        Blocker.Show();
        this.StateHasChanged();

        Model = new ExpandoObject();
        var model = Model as IDictionary<string, object>;
        try
        {
            var flowsResult = await HttpHelper.Get<Dictionary<Guid, string>>($"/api/flow/basic-list");
            var flows = flowsResult.Success ? flowsResult.Data ?? new() : new();

            var librariesResult = await HttpHelper.Get<Dictionary<Guid, string>>($"/api/library/basic-list");
            var libraries = librariesResult.Success ? librariesResult.Data ?? new() : new();

            var nodesResult = await HttpHelper.Get<Dictionary<Guid, string>>($"/api/node/basic-list");
            var nodes = nodesResult.Success ? nodesResult.Data ?? new() : new();

            var tags = (await ClientService.GetTags()).ToDictionary(x => x.Uid, x => x.Name);

            if (rd.DefaultReportPeriod != null)
            {
                if (InputDateRange.DateRanges.TryGetValue(
                        Translater.Instant($"Labels.DateRanges.{rd.DefaultReportPeriod.Value}"), out var period))
                    model["Period"] = period;

                fields.Add(new ElementField()
                {
                    InputType = FormInputType.DateRange,
                    Name = "Period"
                });
            }

            fields.Add(new ElementField
            {
                InputType = FormInputType.Text,
                Name = "Email"
            });

            AddSelectField("Flow", flows, rd.FlowSelection, ref fields, model);
            AddSelectField("Library", libraries, rd.LibrarySelection, ref fields, model);
            AddSelectField("Node", nodes, rd.NodeSelection, ref fields, model);
            AddSelectField("Tags", tags, rd.TagSelection, ref fields, model, anyLabel: "Labels.Any", defaultToAny: true);

            if (rd.Direction)
            {
                fields.Add(new ElementField()
                {
                    InputType = FormInputType.Select,
                    Name = "Direction",
                    Parameters = new()
                    {
                        {
                            "Options", new List<ListOption>()
                            {
                                new() { Label = Translater.Instant("Enums.ReportDirection.Inbound"), Value = 0 },
                                new() { Label = Translater.Instant("Enums.ReportDirection.Outbound"), Value = 1 },
                            }
                        }
                    },
                });
                model["Direction"] = 0;
            }

            foreach (var tf in rd.Fields ?? [])
            {
                if (tf.Type == "Switch")
                {
                    fields.Add(new ElementField
                    {
                        InputType = FormInputType.Switch,
                        Name = tf.Name
                    });
                }
                else if (tf.Type == "Select")
                {
                    var listOptions =
                        JsonSerializer.Deserialize<List<ListOption>>(JsonSerializer.Serialize(tf.Parameters));
                    model[tf.Name] = listOptions.First().Value;
                    fields.Add(new ElementField
                    {
                        InputType = FormInputType.Select,
                        Name = tf.Name,
                        Parameters = new()
                        {
                            { "Options", listOptions }
                        }
                    });

                }
            }
        }
        catch (Exception ex)
        {
            // Ignored
            Logger.Instance.ILog(ex.Message);
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }

        Fields = fields;
        Buttons =
        [
            new ()
            {
                Label = "Pages.Report.Buttons.Generate",
                Clicked = (_, _) => _ = Generate()
            },
            new ()
            {
                Label = "Pages.Report.Buttons.Back",
                Clicked = (_, _) => GoBack()
            }
        ];
        Loaded = true;
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        _needsRendering = false;
        return base.OnAfterRenderAsync(firstRender);
    }
    /// <summary>
    /// The back button was clicked
    /// </summary>
    private void GoBack()
    {
        if (ShowReportOutput)
        {
            Html = string.Empty;
            ShowReportOutput = false;
            return;
        }
        NavigationManager.NavigateTo("/reporting");
    }
    
    /// <summary>
    /// The close button was clicked
    /// </summary>
    private void Close()
    {
        NavigationManager.NavigateTo("/reporting");
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
    
    /// <summary>
    /// Opens the help URL if set
    /// </summary>
    protected void OpenHelp()
    {
        if (string.IsNullOrWhiteSpace(HelpUrl))
            return;
        _ = App.Instance.OpenHelp(HelpUrl.ToLowerInvariant());
    }
    
    /// <summary>
    /// Generates the report
    /// </summary>
    private async Task Generate()
    {
        bool valid = await Editor.Validate();
        if (valid == false)
            return;
        
        Blocker.Show("Generating Report");
        this.StateHasChanged();
        try
        {
            var dict = Model as IDictionary<string, object>;
            object? oEmail = null;
            dict?.TryGetValue("Email", out oEmail);
            bool emailing = string.IsNullOrWhiteSpace(oEmail?.ToString()) == false;
            var result = await HttpHelper.Post<string>($"/api/report/generate/{Uid}", Model);
            if (result.Success == false)
            {
                Toast.ShowError(result.Body?.EmptyAsNull() ?? "Pages.Report.Messages.FailedToGenerateReport");
                return;
            }

            if (emailing)
            {
                Toast.ShowSuccess(Translater.Instant("Pages.Report.Messages.ReportEmailed",
                    new { email = oEmail.ToString() }));
                GoBack();
                return;
            }

            Html = result.Data;
            ShowReportOutput = true;
            StateHasChanged();
            await WaitForRender();
            await jsReports.InvokeVoidAsync("initCharts");
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
        
    }

    /// <summary>
    /// Adds a select field
    /// </summary>
    /// <param name="title">the title of the field</param>
    /// <param name="list">the list of options</param>
    /// <param name="selection">the selection method</param>
    /// <param name="fields">the fields to update</param>
    /// <param name="model">the model to update</param>
    private void AddSelectField(string title, Dictionary<Guid, string> list, ReportSelection selection,
        ref List<IFlowField> fields, IDictionary<string, object> model, string? anyLabel = null, bool defaultToAny = false)
    {
        var listOptions = list.OrderBy(x => x.Value.ToLowerInvariant())
            .Select(x => new ListOption() { Label = x.Value, Value = x.Key }).ToList();

        var label = title == "Tags" ? "Pages.Tags.Title" : null;
        
        switch (selection)
        {
            case ReportSelection.One:
                fields.Add(new ElementField()
                {
                    Name = title,
                    Label = label,
                    InputType = FormInputType.Select,
                    Parameters = new()
                    {
                        {
                            "Options", listOptions
                        }
                    },
                });
                break;
            case ReportSelection.Any:
                model[title] = listOptions.Select(x => x.Value).ToList();
                fields.Add(new ElementField()
                {
                    Name = title,
                    Label = label,
                    InputType = FormInputType.MultiSelect,
                    Parameters = new()
                    {
                        {
                            "Options", listOptions
                        }
                    },
                    Validators = [new Required()]
                });
                break;
            case ReportSelection.AnyOrAll:
                if(defaultToAny)
                    model[title] = new object[] { null }; // any
                else
                    model[title] = listOptions.Select(x => x.Value).ToList();
                fields.Add(new ElementField()
                {
                    Name = title,
                    Label = label,
                    InputType = FormInputType.MultiSelect,
                    Parameters = new()
                    {
                        { "Options", listOptions },
                        { "AnyOrAll", true },
                        { "LabelAny", Translater.Instant(anyLabel?.EmptyAsNull() ?? "Pages.Report.Labels.Combined") }
                    }
                });
                break;
        }
    }
}