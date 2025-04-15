using System.Web;
using FileFlows.Client.Components.Dialogs;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components.Editors;

/// <summary>
/// Script editor
/// </summary>
public partial class ScriptEditor : ModalEditor
{
    /// <summary>
    /// Gets or sets the model
    /// </summary>
    private Script Model { get; set; }
    
    /// <summary>
    /// Gets or sets the modal service
    /// </summary>
    [Inject] private IModalService ModalService { get; set; }

    /// <inheritdoc />
    public override string HelpUrl => "https://fileflows.com/docs/webconsole/config/extensions/scripts";

    private ActionButton[] AdditionalButtons = [];

    private InputCode CodeInput;
    private List<Script> _Shared;


    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Title = Translater.Instant("Pages.Script.Title");
    }

    public override async Task LoadModel()
    {
        if ((Options as ModalEditorOptions)?.Model is Script model)
        {
            if (model.Repository)
            {
                // need to get the code from backend
                var contentResult =
                    await HttpHelper.Get<string>("/api/repository/content?path=" + HttpUtility.UrlEncode(model.Code));
                if (contentResult.Success == false || string.IsNullOrWhiteSpace(contentResult.Data))
                {
                    Close();
                    return;
                }

                // int index = contentResult.Data.IndexOf("#!/bin/bash", StringComparison.InvariantCultureIgnoreCase);
                // model.Code = index > 0 ? contentResult.Data[index..] : contentResult.Data;
                model.Code = contentResult.Data;
                Title = model.Name;
                ReadOnly = true;
            }
            else if (model.Uid == Guid.Empty)
                InitializeModel(model);

            Model = model;
        }
        else
        {
            var uid = GetModelUid();

            var result = await HttpHelper.Get<Script>("/api/script/" + uid);
            if (result.Success == false || result.Data == null)
            {
                Container.HideBlocker();
                Close();
            }

            InitializeModel(result.Data);
        }

        if (ReadOnly == false && Model.Language == ScriptLanguage.JavaScript)
        {
            AdditionalButtons =
            [
                new()
                {
                    Uid = "import",
                    Label = "Labels.Import",
                    Clicked = async (_, _) =>
                    {
                        string code = await CodeInput.GetCode() ?? string.Empty;
                        var shared = await GetShared();
                        var available = shared
                            .Where(x => code.IndexOf("Shared/" + x.Name, StringComparison.Ordinal) < 0)
                            .Select(x => x.Name).ToList();
                        if (available.Any() == false)
                        {
                            feService.Notifications.ShowWarning("Dialogs.ImportScript.Messages.NoMoreImports");
                            return;
                        }

                        var result = await ModalService.ShowModal<ImportScript, string[]>(new ImportScriptOptions()
                        {
                            AvailableScripts = available
                        });
                        if (result.IsFailed || result.Value == null || result.Value.Length == 0)
                            return;
                        
                        await CodeInput.AddImports(result.Value.ToList());
                    }
                }
            ];
        }

        StateHasChanged();
    }

    /// <summary>
    /// Gets the shared scripts
    /// </summary>
    /// <returns>the shared scripts</returns>
    private async Task<List<Script>> GetShared()
    {
        if (_Shared == null)
        {
            var result = await HttpHelper.Get<List<Script>>("/api/script/list/Shared");
            if (result.Success)
                this._Shared = result.Data;
        }

        return _Shared ?? new ();
    }

    /// <summary>
    /// Opens the script editor to edit a specific script
    /// </summary>
    /// <param name="model">the script to edit</param>
    /// <returns>true if the script was saved, otherwise false</returns>
    public void InitializeModel(Script model)
    {
        Model = model;
        bool flowScript = model.Type == ScriptType.Flow;
        ReadOnly = model.Repository || feService.HasRole(UserRole.Scripts) == false;


        if (string.IsNullOrEmpty(model.Code))
        {
            if (model.Language == ScriptLanguage.JavaScript)
            {
                model.Code = flowScript
                    ? @"
/**
 * @description The description of this script
 * @param {int} NumberParameter Description of this input
 * @output Description of output 1
 * @output Description of output 2
 */
function Script(NumberParameter)
{
    return 1;
}
"
                    : @"
import { FileFlowsApi } from 'Shared/FileFlowsApi';

let ffApi = new FileFlowsApi();
";
            }
            else if (model.Language == ScriptLanguage.Batch)
            {
                model.Code = @"
REM This is a template batch file

REM Replace {file.FullName} and {file.Orig.FullName} with actual values
SET WorkingFile={file.FullName}
SET OriginalFile={file.Orig.FullName}

REM Example commands using the variables
echo Working on file: %WorkingFile%
echo Original file location: %OriginalFile%

REM Add your actual batch commands below
REM Example: Copy the working file to a backup location
REM copy ""%WorkingFile%"" ""C:\Backup\%~nxWorkingFile%""

REM Set the exit code to 1
EXIT /B 1".Trim();
            }
            else if (model.Language == ScriptLanguage.PowerShell)
            {
                model.Code = @"
# This is a template PowerShell script

# Replace {WorkingFile} and {OriginalFile} with actual values
$WorkingFile = '{WorkingFile}'
$OriginalFile = '{OriginalFile}'

# Example commands using the variables
Write-Output ""Working on file: $WorkingFile""
Write-Output ""Original file location: $OriginalFile""

# Add your actual PowerShell commands below
# Example: Copy the working file to a backup location
# Copy-Item -Path $WorkingFile -Destination ""C:\Backup\$([System.IO.Path]::GetFileName($WorkingFile))""

# Set the exit code to 1
exit 1".Trim();
            }
            else if (model.Language == ScriptLanguage.Shell)
            {
                model.Code = @"
# This is a template shell script

# Replace {file.FullName} and {file.Orig.FullName} with actual values
WorkingFile=""{file.FullName}""
OriginalFile=""{file.Orig.FullName}""

# Example commands using the variables
echo ""Working on file: $WorkingFile""
echo ""Original file location: $OriginalFile""

# Add your actual shell commands below
# Example: Copy the working file to a backup location
# cp ""$WorkingFile"" ""/path/to/backup/$(basename \""$WorkingFile\"")""

# Set the exit code to 1
exit 1".Trim();
            }
            else if (model.Language == ScriptLanguage.CSharp)
            {
                model.Code = @"
// A C# script will have full access to the executing flow.
// Return the output to call next

// Replace these variables with actual values
string workingFile = Variables.file.FullName;
string originalFile = Variables.file.Orig.FullName;

// Example code using the variables
Console.WriteLine($""Working on file: {workingFile}"");
Console.WriteLine($""Original file location: {originalFile}"");

// Add your actual C# code below
return 1;
".Trim();
            }
        }
        else if(model.Language == ScriptLanguage.JavaScript)
        {
            model.Code = ScriptParser.GetCodeWithCommentBlock(model, true);
        }

        model.Code = model.Code.Replace("\r\n", "\n").Trim();
        string langTitle = model.Language switch
        {
            ScriptLanguage.CSharp => "C#",
            _ => model.Language.ToString()
        };
        
        if (ReadOnly)
        {
            Title = Translater.Instant("Pages.Script.LanguageTitle", new { Language = langTitle }) + ":" + model.Name;
        }
        
        StateHasChanged();
    }
    
    /// <summary>
    /// Saves the script
    /// </summary>
    public override async Task Save()
    {
        Container.ShowBlocker();
        
        try
        {
            var saveResult = await HttpHelper.Post<Script>($"/api/script", Model);
            if (saveResult.Success == false)
            {
                feService.Notifications.ShowError(saveResult.Body?.EmptyAsNull() ?? 
                                                  Translater.Instant("ErrorMessages.SaveFailed"));
                return;
            }

            await Task.Delay(500); // give change for script to get updated in list

            TaskCompletionSource.TrySetResult(saveResult.Data);
        }
        finally
        {
             Container.HideBlocker();
        }
        
    }
}