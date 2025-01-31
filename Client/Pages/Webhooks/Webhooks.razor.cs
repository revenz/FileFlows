using FileFlows.Client.Components;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

/// <summary>
/// A page for user to manage their webhooks
/// </summary>
public partial class Webhooks : ListPage<Guid, Webhook>
{
    /// <summary>
    /// Gets the API URL endpoint for the webhooks
    /// </summary>
    public override string ApiUrl => "/api/webhook";
    /// <summary>
    /// Gets or sets the script browser component instance
    /// </summary>
    private RepositoryBrowser ScriptBrowser { get; set; }

    private Webhook EditingItem = null;
    private string lblRoute, lblMethod;
    private string BaseRoute;
    /// <summary>
    /// Gets or sets the clipboard service
    /// </summary>
    [Inject] IClipboardService ClipboardService { get; set; }   

    /// <summary>
    /// Gets if they are licensed for this page
    /// </summary>
    /// <returns>if they are licensed for this page</returns>
    protected override bool Licensed()
        => Profile.LicensedFor(LicenseFlags.Webhooks); 

    /// <summary>
    /// Initializes the webhooks page
    /// </summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        lblRoute = Translater.Instant("Pages.Webhooks.Columns.Route");
        lblMethod = Translater.Instant("Pages.Webhooks.Columns.Method");
        BaseRoute = NavigationManager.BaseUri.TrimEnd('/') + "/";
        int pathIndex = BaseRoute.LastIndexOf("/webhooks", StringComparison.OrdinalIgnoreCase);
        if (pathIndex >= 0)
            BaseRoute = BaseRoute.Substring(0, pathIndex);

        if (BaseRoute.EndsWith("/") == false)
            BaseRoute += "/";
        BaseRoute += "webhook/";
    }

    /// <summary>
    /// Copies the webhooks route to the clipboard
    /// </summary>
    /// <param name="item">the webhook to copy</param>
    async Task CopyToClipboard(Webhook item)
    {
        string route = BaseRoute + item.Route;
        await ClipboardService.CopyToClipboard(route);
    }

    /// <summary>
    /// Opens the editor to add a new webhook
    /// </summary>
    /// <returns>if the add was successful or not</returns>
    private Task Add()
        => Edit(new Webhook());

    /// <summary>
    /// Opens an editor to edit a webhook
    /// </summary>
    /// <param name="Webhook">the webhook being editted</param>
    /// <returns>if the edit was successful or not</returns>
    public override async Task<bool> Edit(Webhook Webhook)
    {
        this.EditingItem = Webhook;
        List<IFlowField> fields = new ();
        fields.Add(new ElementField
        {
            InputType = FormInputType.Text,
            Name = nameof(Webhook.Name),
            Validators = new List<Validator> {
                new Required()
            },
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Text,
            Name = nameof(Webhook.Route),
            Validators = new List<Validator>
            {
                new Required(),
                new Pattern() { Expression = @"^[a-zA-Z0-9\-._+]+$" }
            },
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Select,
            Name = nameof(Webhook.Method),
            Parameters = new Dictionary<string, object>
            {
                { "Options", new List<ListOption>()
                    {
                        new () { Label = "GET", Value = (int)HttpMethod.Get },
                        new () { Label = "POST", Value = (int)HttpMethod.Post },
                        //new () { Label = "PUT", Value = (int)HttpMethod.Put },
                        //new () { Label = "DELETE", Value = (int)HttpMethod.Delete },
                    }
                }
            },
        });
        fields.Add(new ElementField
        {
            InputType = Plugin.FormInputType.Code,
            Name = nameof(Webhook.Code)
        });
        await Editor.Open(new () { TypeName = "Pages.Webhook", Title = "Pages.Webhook.Title", 
            Fields = fields, Model = Webhook, SaveCallback = Save, Large = true, FullWidth = true, HideFieldsScroller = true 
        });
        return false;
    }

    /// <summary>
    /// Saves a webhook
    /// </summary>
    /// <param name="model">the model of the webhook being saved</param>
    /// <returns>if the save was successful or not</returns>
    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var saveResult = await HttpHelper.Post<Webhook>($"{ApiUrl}", model);
            if (saveResult.Success == false)
            {
                Toast.ShowEditorError( saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
                return false;
            }

            int index = this.Data.FindIndex(x => x.Uid == saveResult.Data.Uid);
            if (index < 0)
                this.Data.Add(saveResult.Data);
            else
                this.Data[index] = saveResult.Data;
            await this.Load(saveResult.Data.Uid);

            return true;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Opens the scripts browser
    /// </summary>
    async Task Browser()
    {
        bool result = await ScriptBrowser.Open();
        if (result)
            await this.Refresh();
    }
}