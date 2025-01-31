using System.Text.RegularExpressions;
using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;
using FileFlows.ScriptExecution;
using Logger = FileFlows.Shared.Logger;

namespace FileFlows.Client.Components.ScriptEditor;

/// <summary>
/// An editor used to edit scripts 
/// </summary>
public class ScriptEditor
{
    /// <summary>
    /// The editor instance
    /// </summary>
    private readonly Editor Editor;
    
    /// <summary>
    /// The script importer
    /// </summary>
    private readonly Dialogs.ImportScript ScriptImporter;
    
    /// <summary>
    /// The callback to call when the script editor is saved
    /// </summary>
    private readonly Editor.SaveDelegate SaveCallback;

    /// <summary>
    /// The blocker
    /// </summary>
    private readonly Blocker Blocker;

    public ScriptEditor(Editor editor, Dialogs.ImportScript scriptImporter, Editor.SaveDelegate saveCallback = null, Blocker blocker = null)
    {
        this.Editor = editor;
        this.ScriptImporter = scriptImporter;
        this.SaveCallback = saveCallback;
        this.Blocker = blocker;
    }
    
    
    /// <summary>
    /// Opens the script editor to edit a specific script
    /// </summary>
    /// <param name="item">the script to edit</param>
    /// <returns>true if the script was saved, otherwise false</returns>
    public async Task<bool> Open(Script item)
    {
        List<IFlowField> fields = new();
        bool flowScript = item.Type == ScriptType.Flow;

        string editorLanguage = item.Language.ToString().ToLowerInvariant();

        if (string.IsNullOrEmpty(item.Code))
        {
            if (item.Language == ScriptLanguage.JavaScript)
            {
                item.Code = flowScript
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
            else if (item.Language == ScriptLanguage.Batch)
            {
                editorLanguage = "bat";
                item.Code = @"
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
            else if (item.Language == ScriptLanguage.PowerShell)
            {
                item.Code = @"
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
            else if (item.Language == ScriptLanguage.Shell)
            {
                item.Code = @"
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
            else if (item.Language == ScriptLanguage.CSharp)
            {
                item.Code = @"
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
        else if(item.Language == ScriptLanguage.JavaScript)
        {
            item.Code = ScriptParser.GetCodeWithCommentBlock(item, true);
        }

        item.Code = item.Code.Replace("\r\n", "\n").Trim();

        bool readOnly = item.Repository;
        string langTitle = item.Language switch
        {
            ScriptLanguage.CSharp => "C#",
            _ => item.Language.ToString()
        };
        string title = Translater.Instant("Pages.Script.LanguageTitle", new { Language = langTitle });
        
        if (readOnly)
        {
            title += ": " + item.Name;
        }
        else if(item.Language == ScriptLanguage.JavaScript)
        {
            if (item.Name != CommonVariables.FILE_DISPLAY_NAME)
            {
                fields.Add(new ElementField
                {
                    InputType = FormInputType.Text,
                    Name = nameof(item.Name),
                    Validators = flowScript ? [new Required()] : []
                });
            }
            else
            {
                title = Translater.Instant("Dialogs.ScriptLanguage.Labels.FileDisplayName");
            }
        }
        else if(item.Type == ScriptType.Flow)
        {
            var panel = new ElementPanel()
            {
                Columns = 2
            };
            fields.Add(panel);

            var leftPanel = new ElementPanel();
            var rightPanel = new ElementPanel();
            panel.Fields.Add(leftPanel);
            panel.Fields.Add(rightPanel);
                
            leftPanel.Fields.Add(new ElementField
            {
                InputType = FormInputType.Text,
                Name = nameof(item.Name),
                Validators = flowScript ? [ new Required() ] : []
            });
            rightPanel.Fields.Add(new ElementField
            {
                InputType = FormInputType.KeyValueInt,
                Name = nameof(item.Outputs),
                RowSpan = 2,
                HideLabel = true,
                Parameters = new ()
                {
                    { nameof(InputKeyValueInt.HideKeyValueLabels), true }
                }
            });
            leftPanel.Fields.Add(new ElementField
            {
                InputType = FormInputType.TextArea,
                Name = "Description",
                ColSpan = 1,
                FlexGrow = true
            });
        }
        else
        {
            // non-js/non flow script       
            fields.Add(new ElementField
            {
                InputType = FormInputType.Text,
                Name = nameof(item.Name),
                Validators = flowScript ? [ new Required() ] : []
            });
            fields.Add(new ElementField
            {
                InputType = FormInputType.TextArea,
                Name = "Description"
            });
        }

        fields.Add(new ElementField
        {
            InputType = FormInputType.Code,
            Name = "Code",
            Parameters = new ()
            {
                { nameof(InputCode.Language), editorLanguage }
            },
            Validators = item.Type == ScriptType.Flow ? new List<Validator>
            {
                //new ScriptValidator()
            } : new List<Validator>()
        });

        var result = await Editor.Open(new()
        {
            TypeName = "Pages.Script", Title = title, Fields = fields, Model = item, 
            Large = true,
            ReadOnly = readOnly,
            SaveCallback = SaveCallback ?? Save, HelpUrl = "https://fileflows.com/docs/webconsole/extensions/scripts",
            AdditionalButtons = readOnly || item.Language != ScriptLanguage.JavaScript || item.Type == ScriptType.Shared ? null : new ActionButton[]
            {
                new ()
                {
                    Label = "Labels.Import", 
                    Clicked = (sender, e) => _ = OpenImport(sender, e)
                }
            }
        });

        return result.Success;
    }
    
    
    private List<Script> _Shared;

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
    /// Opens the import script dialog
    /// </summary>
    /// <param name="sender">the sender</param>
    /// <param name="e">the event arguments</param>
    private async Task OpenImport(object sender, EventArgs e)
    {
        if (sender is Editor editor == false)
            return;

        var codeInput = editor.FindInput<InputCode>("Code");
        if (codeInput == null)
            return;

        string code = await codeInput.GetCode() ?? string.Empty;
        var shared = await GetShared();
        var available = shared.Where(x => code.IndexOf("Shared/" + x.Name, StringComparison.Ordinal) < 0).Select(x => x.Name).ToList();
        if (available.Any() == false)
        {
            Toast.ShowWarning("Dialogs.ImportScript.Messages.NoMoreImports");
            return;
        }

        List<string> import = await ScriptImporter.Show(available);
        Logger.Instance.ILog("Import", import);
        await codeInput.AddImports(import);
    }
    
    

    async Task<bool> Save(ExpandoObject model)
    {
        Blocker?.Show();

        try
        {
            var saveResult = await HttpHelper.Post<Script>($"/api/script", model);
            if (saveResult.Success == false)
            {
                Toast.ShowEditorError(saveResult.Body?.EmptyAsNull() ?? Translater.Instant("ErrorMessages.SaveFailed"));
                return false;
            }

            return true;
        }
        finally
        {
            Blocker?.Hide();
        }
    }
}