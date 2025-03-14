namespace FileFlows.Client.Components;

using Microsoft.AspNetCore.Components;
using FileFlows.Shared;

public partial class Blocker : ComponentBase
{

    private int Count = 0;
    
    private bool _Visible;

    [Parameter]
    public bool Visible
    {
        get => _Visible;
        set => _Visible = value;
    }

    public string Message { get; set; } = "";

    public void Show(string message = "")
    {
        // nulls are yucky
        Message ??= "";
        message ??= "";


        if (++Count < 1)
            Count = 1;
        if (Translater.NeedsTranslating(message))
            message = Translater.Instant(message);

        if (this.Visible == true && Message == message)
            return;

        this.Visible = true;
        this.Message = message;
        this.StateHasChanged();
    }

    public void Hide()
    {
        if (--Count > 0)
            return;
        
        if (this.Visible == false)
            return;
        this.Message = "";
        this.Visible = false;
        this.StateHasChanged();
    }

}