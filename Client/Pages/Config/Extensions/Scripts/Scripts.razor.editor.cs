using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Client.Components.ScriptEditor;
using FileFlows.Plugin;

namespace FileFlows.Client.Pages;

public partial class Scripts
{
    /// <summary>
    /// The script importer
    /// </summary>
    private Components.Dialogs.ImportScript ScriptImporter;

    /// <summary>
    /// Editor for a Script
    /// </summary>
    /// <param name="item">the script to edit</param>
    /// <returns>the result of the edit</returns>
    public override async Task<bool> Edit(Script item)
    {
        if (item.Uid != Guid.Empty)
        {
            // load the complete DockerMod
            Blocker.Show();
            var dmResult = await HttpHelper.Get<Script>($"{ApiUrl}/{item.Uid}");
            Blocker.Hide();

            if (dmResult.Success == false || dmResult.Data == null)
            {
                ShowEditHttpError(dmResult, "Script not found");
                return false;
            }

            item = dmResult.Data;
        }
        
        
        var editor = new ScriptEditor(Editor, ScriptImporter, saveCallback: Save);
        await editor.Open(item);
        
        return false;
    }

    /// <summary>
    /// Copies the values from one script into another for editing
    /// </summary>
    /// <param name="source">the source to duplicate</param>
    /// <param name="destination">the destination</param>
    private void CopyInto(Script source, Script destination)
    {
        destination.Name = source.Name;
        destination.Description = source.Description;
        destination.Code = source.Code;
        destination.Help = source.Help;
        destination.Author = source.Author;
        destination.Type = source.Type;
        destination.Language = source.Language;
        //destination.Outputs =  source.Outputs?.Select(x => new ScriptOutput() { Description = x.Description, Index = x.Index })?.ToList() ?? [];
        destination.Outputs =  source.Outputs?.Select(x => new KeyValuePair<int, string>(x.Key, x.Value))?.ToList() ?? [];
        destination.Parameters = source.Parameters?.Select(x => new ScriptParameter()
            { Name = x.Name, Type = x.Type, Description = x.Description, Options = x.Options})?.ToList() ?? [];
        destination.Path = source.Path;
        destination.Repository = source.Repository;
        destination.Revision = source.Revision;
        destination.LatestRevision = source.LatestRevision;
        destination.MinimumVersion = source.MinimumVersion;
        destination.UsedBy =
            source.UsedBy?.Select(x => new ObjectReference() { Name = x.Name, Type = x.Type, Uid = x.Uid })?.ToList() ??
            [];
        destination.Uid = source.Uid;
        destination.DateCreated = source.DateCreated;
        destination.DateModified = source.DateModified;
    }

    private async Task OpenImport(object sender, EventArgs e)
    {
        if (sender is Editor editor == false)
            return;

        var codeInput = editor.FindInput<InputCode>("Code");
        if (codeInput == null)
            return;

        string code = await codeInput.GetCode() ?? string.Empty;
        var available = DataShared.Where(x => code.IndexOf("Shared/" + x.Name) < 0).Select(x => x.Name).ToList();
        if (available.Any() == false)
        {
            Toast.ShowWarning("Dialogs.ImportScript.Messages.NoMoreImports");
            return;
        }

        List<string> import = await ScriptImporter.Show(available);
        Logger.Instance.ILog("Import", import);
        await codeInput.AddImports(import);
    }
}