using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Inputs;

public partial class InputWrapper : ComponentBase
{
    [Parameter] public IInput Input { get; set; }

    [Parameter] public bool NoSpacing { get; set;}

    /// <summary>
    /// Gets or sets the clipboard service
    /// </summary>
    [Inject] IClipboardService ClipboardService { get; set; }   

    [Parameter] public RenderFragment ChildContent { get; set; }
    
    /// <summary>
    /// Gets or sets if this is an error input
    /// </summary>
    [Parameter] public bool Error { get; set; }

    private string HelpHtml = string.Empty;
    private string CurrentHelpText;
    private string lblTooltip, lblCustomField;
    
    protected override void OnInitialized()
    {
        this.lblTooltip = Translater.Instant("Labels.CopyToClipboard");
        this.lblCustomField = Translater.Instant("Labels.CustomField");
        InitHelpText();
    }

    protected override void OnParametersSet()
    {
        if (CurrentHelpText != Input?.Help)
        {
            InitHelpText();
            this.StateHasChanged();
        }
    }

    private void InitHelpText()
    {
        CurrentHelpText = Input?.Help;
        // string help = Regex.Replace(Input?.Help ?? string.Empty, "<.*?>", string.Empty);
        // foreach (Match match in Regex.Matches(help, @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()!@:%_\+.~#?&\/\/=]*)", RegexOptions.Multiline))
        // {
        //     help = help.Replace(match.Value, $"<a rel=\"noreferrer\" target=\"_blank\" href=\"{HttpUtility.HtmlAttributeEncode(match.Value)}\">{HttpUtility.HtmlEncode(match.Value)}</a>");
        // }
        if (string.IsNullOrEmpty(Input?.Help))
        {
            this.HelpHtml = string.Empty;
        }
        else
        {
            string help = Markdig.Markdown.ToHtml(Input.Help).Trim();
            if (help.StartsWith("<p>") && help.EndsWith("</p>"))
                help = help[3..^4].Trim();
            help = help.Replace("<a ", "<a onclick=\"ff.openLink(event);return false;\" ");
            this.HelpHtml = help;
        }
    }
    
    async Task CopyToClipboard()
    {
        await ClipboardService.CopyToClipboard(this?.Input?.Field?.CopyValue);
    }
}