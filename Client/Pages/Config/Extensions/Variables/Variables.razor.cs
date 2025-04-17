using FileFlows.Client.Components.Editors;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Pages;

public partial class Variables : ListPage<Guid, Variable>, IDisposable
{
    
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }
    
    public override string ApiUrl => "/api/variable";

    private string lblValue;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Profile = feService.Profile.Profile;
        Layout.SetInfo(Translater.Instant("Pages.Variables.Title"), "fas fa-at");
        base.OnInitialized(false);
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

    /// <summary>
    /// Adds a new tag
    /// </summary>
    private async Task Add()
    {
        await ModalService.ShowModal<VariableEditor>(new ModalEditorOptions()
        {
            Model = new Variable()
            {
            }
        });
    }


    /// <inheritdoc />
    public override async Task<bool> Edit(Variable variable)
    {
        await ModalService.ShowModal<VariableEditor>(new ModalEditorOptions()
        {
            Model = new Variable()
            {
                Uid = variable.Uid,
                Name = variable.Name,
                Value = variable.Value
            }
        });
        return false;
    }

    /// <summary>
    /// Disposes of the component
    /// </summary>
    public void Dispose()
    {
        feService.Variable.VariablesUpdated -= VariableOnVariablesUpdated;
    }

}