using FileFlows.Client.Components;
using FileFlows.Plugin;
using FileFlows.Client.Components.Inputs;

namespace FileFlows.Client.Pages.Reseller;

/// <summary>
/// Editor for ResellerFlows
/// </summary>
public partial class ResellerFlows : ListPage<Guid, ResellerFlow>
{
    /// <summary>
    /// Gets the flows
    /// </summary>
    /// <returns>the list of flows</returns>
    private Task<RequestResult<Dictionary<Guid, string>>> GetFlows()
        => HttpHelper.Get<Dictionary<Guid, string>>("/api/flow/basic-list?type=Standard");
    
    /// <summary>
    /// Opens the editor
    /// </summary>
    /// <param name="item">the reseller flow to edit</param>
    /// <returns>true if the editor was saved, otherwise false</returns>
    private async Task<bool> OpenEditor(ResellerFlow item)
    {
        Blocker.Show();
        var flowResult = await GetFlows();
        Blocker.Hide();
        if (flowResult.Success == false || flowResult.Data?.Any() != true)
        {
            ShowEditHttpError(flowResult, "Pages.Libraries.ErrorMessages.NoFlows");
            return false;
        }
        var flowOptions = flowResult.Data
            .Select(x => new ListOption { Value = new ObjectReference { Name = x.Value, Uid = x.Key, Type = typeof(Flow).FullName! }, Label = x.Value });
        
        var fields = new List<IFlowField>();
        fields.Add(new ElementField()
        {
            Name = nameof(item.Name),
            InputType = FormInputType.Text
        });
        fields.Add(new ElementField()
        {
            Name = nameof(item.Description),
            InputType = FormInputType.TextArea,
            Parameters = new ()
            {
                { nameof(InputTextArea.Rows), 3}
            }
        });
        fields.Add(new ElementField()
        {
            InputType = FormInputType.HorizontalRule
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Select,
            Name = nameof(item.Flow),
            Parameters = new Dictionary<string, object>{
                { "Options", flowOptions.OrderBy(x => x.Label.ToLowerInvariant()).ToList() }
            },
            Validators = new List<Validator>
            {
                new Required()
            }
        });
        fields.Add(new ElementField()
        {
            Name = nameof(item.Icon),
            InputType = FormInputType.IconPicker
        });
        fields.Add(new ElementField()
        {
            InputType = FormInputType.HorizontalRule
        });
        fields.Add(new ElementField()
        {
            Name = nameof(item.Tokens),
            InputType = FormInputType.Int
        });
        fields.Add(new ElementField()
        {
            Name = nameof(item.MaxFileSize),
            InputType = FormInputType.FileSize
        });
        fields.Add(new ElementField()
        {
            Name = nameof(item.Extensions),
            InputType = FormInputType.StringArray
        });
        fields.Add(new ElementField
        {
            InputType = FormInputType.Select,
            Name = nameof(item.PreviewMode),
            Parameters = new Dictionary<string, object>{
                { 
                    "Options", new List<ListOption>
                    {
                        new () { Label = "List", Value = ResellerPreviewMode.List },
                        new () { Label = "Images", Value = ResellerPreviewMode.Images },   
                    }
                }
            },
            Validators = new List<Validator>
            {
                new Required()
            }
        });
        fields.Add(new ElementField()
        {
            InputType = FormInputType.HorizontalRule
        });
        fields.Add(new ElementField()
        {
            InputType = FormInputType.CustomFields,
            Name = nameof(item.Fields)
        });

        
        await Editor.Open(new()
        {
            TypeName = "Pages.Resellers.Flows", Title = "Pages.Resellers.Flows.Single", Model = item,
            SaveCallback = Save, Fields = fields,
            HelpUrl = "https://fileflows.com/docs/webconsole/reseller/flows"
        });
        return true;
    }
    

    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var saveResult = await HttpHelper.Post<ResellerFlow>(ApiUrl, model);
            if (saveResult.Success == false)
            {
                Toast.ShowEditorError( Translater.TranslateIfNeeded(saveResult.Body?.EmptyAsNull() ?? "ErrorMessages.SaveFailed"));
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
}
