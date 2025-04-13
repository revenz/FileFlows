using FileFlows.Client.Components.Inputs;
using FileFlows.Plugin;

namespace FileFlows.Client.Components.Editors;

/// <summary>
/// Script editor
/// </summary>
public partial class ScriptEditor : ModalEditor
{
    /// <inheritdoc />
    public override IModalOptions Options { get; set; }

    private bool ReadOnly = false;

    /// <summary>
    /// Gets or sets the UID of script
    /// </summary>
    private Guid Uid { get; set; }

    /// <summary>
    /// Gets or sets the name of script
    /// </summary>
    private string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of script
    /// </summary>
    private string Description { get; set; }
    
    /// <summary>
    /// Gets or sets the javascript code of the script
    /// </summary>
    public string Code { get; set; }
    /// <summary>
    /// Gets or sets a list of outputs for the script
    /// </summary>
    public List<KeyValuePair<int, string>> Outputs { get; set; } = new ();

    /// <summary>
    /// Gets or sets if this script is a from a repository and cannot be modified
    /// </summary>
    public bool Repository { get; set; }

    /// <summary>
    /// Gets or sets the type of script
    /// </summary>
    public ScriptType Type { get; set; }

    /// <summary>
    /// Gets or sets the Language of script
    /// </summary>
    public ScriptLanguage Language { get; set; }
    
    /// <summary>
    /// Gets or sets the container
    /// </summary>
    public ViContainer Container { get; set; }

    /// <inheritdoc />
    protected override string HelpUrl => "https://fileflows.com/docs/webconsole/config/extensions/scripts";

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        lblTitle = Translater.Instant("Pages.Script.Title");

        var uid = GetModelUid();
        var result  = await HttpHelper.Get<Script>("/api/script/" + uid);
        InitializeModel(result.Data);
    }


    /// <summary>
    /// Opens the script editor to edit a specific script
    /// </summary>
    /// <param name="model">the script to edit</param>
    /// <returns>true if the script was saved, otherwise false</returns>
    public void InitializeModel(Script model)
    {
        List<IFlowField> fields = new();
        bool flowScript = model.Type == ScriptType.Flow;
        Uid = model.Uid;
        Code = model.Code;
        Language = model.Language;
        ReadOnly = model.Repository || feService.HasRole(UserRole.Scripts) == false;
        Name = model.Name ?? string.Empty;
        Description = model.Description ?? string.Empty;
        Type = model.Type;
        Outputs = model.Outputs;

        string editorLanguage = Language.ToString().ToLowerInvariant();

        if (string.IsNullOrEmpty(Code))
        {
            if (Language == ScriptLanguage.JavaScript)
            {
                Code = flowScript
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
            else if (Language == ScriptLanguage.Batch)
            {
                editorLanguage = "bat";
                Code = @"
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
            else if (Language == ScriptLanguage.PowerShell)
            {
                Code = @"
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
            else if (Language == ScriptLanguage.Shell)
            {
                Code = @"
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
            else if (Language == ScriptLanguage.CSharp)
            {
                Code = @"
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
        else if(Language == ScriptLanguage.JavaScript)
        {
            Code = ScriptParser.GetCodeWithCommentBlock(model, true);
        }

        Code = Code.Replace("\r\n", "\n").Trim();
        string langTitle = Language switch
        {
            ScriptLanguage.CSharp => "C#",
            _ => Language.ToString()
        };
        
        if (ReadOnly)
        {
            lblTitle = Translater.Instant("Pages.Script.LanguageTitle", new { Language = langTitle }) + ":" + Name;
        }
    }
    
    /// <summary>
    /// Saves the script
    /// </summary>
    private async Task Save()
    {
        Container.ShowBlocker();
        

        try
        {
            var saveResult = await HttpHelper.Post<Script>($"/api/script", new 
            {
                 Uid,
                 Name,
                 Code,
                 Type,
                 Language,
            });
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