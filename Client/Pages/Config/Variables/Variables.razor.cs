using FileFlows.Client.Components;

namespace FileFlows.Client.Pages;

public partial class Variables : ListPage<Guid, Variable>, IDisposable
{
    public override string ApiUrl => "/api/variable";

    private Variable EditingItem = null;
    private string lblValue, lblTitle;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Profile = feService.Profile.Profile;
        base.OnInitialized(false);
        lblTitle = Translater.Instant("Pages.Variables.Title");
        lblValue = Translater.Instant("Pages.Flow.Fields.VariablesValue");
        feService.Variable.VariablesUpdated += VariableOnVariablesUpdated ;
        Data = feService.Variable.Variables;
    }

    /// <summary>
    /// Called when the variables are updated
    /// </summary>
    /// <param name="obj"></param>
    private void VariableOnVariablesUpdated(List<Variable> obj)
    {
        Data = obj;
        StateHasChanged();
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Table.SetData(Data);
            StateHasChanged();
        }

        base.OnAfterRender(firstRender);
    }

    private async Task Add()
    {
        await Edit(new Variable());
    }


    /// <inheritdoc />
    public override async Task<bool> Edit(Variable variable)
    {
        this.EditingItem = variable;
        List<IFlowField> fields = new ();
        fields.Add(new ElementField
        {
            InputType = FileFlows.Plugin.FormInputType.Text,
            Name = nameof(variable.Name),
            HideLabel = true,
            Validators = new List<Validator> {
                new Required()
            },
            
        });
        fields.Add(new ElementField
        {
            InputType = FileFlows.Plugin.FormInputType.TextArea,
            FlexGrow = true,
            HideLabel = true,
            Name = nameof(variable.Value),
            Validators = new List<Validator> {
                new Required()
            }
        });
        await Editor.Open(new () { TypeName = "Pages.Variable", Title = "Pages.Variable.Title", 
            Fields = fields, Model = variable, SaveCallback = Save,
            FullWidth = true
        });
        return false;
    }

    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();

        try
        {
            var saveResult = await HttpHelper.Post<Variable>($"{ApiUrl}", model);
            if (saveResult.Success == false)
            {
                Toast.ShowEditorError( saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
                return false;
            }

            return true;
        }
        finally
        {
            Blocker.Hide();
            this.StateHasChanged();
        }
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Variable.VariablesUpdated -= VariableOnVariablesUpdated;
    }

}