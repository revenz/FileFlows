using System.Web;
using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Client.Components.ScriptEditor;
using FileFlows.Client.Helpers;
using FileFlows.Plugin;
using Humanizer;

namespace FileFlows.Client.Pages;

/// <summary>
/// Page for resources
/// </summary>
public partial class Resources : ListPage<Guid, Resource>
{
    /// <inheritdoc />
    public override string ApiUrl => "/api/resource";
    
    /// <summary>
    /// Gets if they are licensed for this page
    /// </summary>
    /// <returns>if they are licensed for this page</returns>
    protected override bool Licensed()
        => Profile.LicensedFor(LicenseFlags.AutoUpdates); 
    
    /// <summary>
    /// Adds a new task
    /// </summary>
    private async Task Add()
    {
        await Edit(new Resource());
    }
    
    /// <inheritdoc />
    public override async Task<bool> Edit(Resource item)
    {
        List<IFlowField> fields = new ();
        Blocker.Show();
        try
        {

            // need to actually load the item
            if (item.Uid != Guid.Empty)
            {
                var result = await HttpHelper.Get<Resource>(ApiUrl + "/" + item.Uid);
                if (result is { Success: true, Data: not null })
                    item = result.Data;
            }

            fields.Add(new ElementField
            {
                InputType = FormInputType.Text,
                Name = nameof(item.Name),
                Validators = new List<Validator>
                {
                    new Required(),
                    new SafeName()
                }
            });
            fields.Add(new ElementField
            {
                InputType = FormInputType.Binary,
                Name = "FileData",
                Validators = new List<Validator>
                {
                    new Required()
                }
            });
        }
        finally
        {
            Blocker.Hide();
        }

        await Editor.Open(new()
        {
            TypeName = "Pages.Resources", Title = "Pages.Resources.Singular", Fields = fields, Model = new
            {
                item.Uid,
                item.Name,
                FileData = item is { MimeType: not null, Data: not null } ? new FileData()
                {
                    MimeType = item.MimeType,
                    Content = item.Data
                } : null
            },
            SaveCallback = Save
        });
        
        return false;
    }
    
    
    /// <summary>
    /// Saves a task
    /// </summary>
    /// <param name="model">the model of the task to save</param>
    /// <returns>true if successful and if the editor should be closed</returns>
    async Task<bool> Save(ExpandoObject model)
    {
        Blocker.Show();
        this.StateHasChanged();
        var resource = new Resource();
        var dict = model as IDictionary<string, object>;
        resource.Name = dict["Name"].ToString() ?? string.Empty;
        resource.Uid = (Guid)dict["Uid"];
        if (dict.TryGetValue("FileData", out var value))
        {
            var data = (FileData)value;
            resource.MimeType = data.MimeType;
            resource.Data = data.Content;
        }

        if (string.IsNullOrWhiteSpace(resource.MimeType) || resource.Data == null || resource.Data.Length == 0)
            return false;

        try
        {
            var saveResult = await HttpHelper.Post<Resource>($"{ApiUrl}", resource);
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
    /// Gets the icon for a task
    /// </summary>
    /// <param name="resource">the task</param>
    /// <returns>the icon</returns>
    private string GetIcon(Resource resource)
    {
        if (resource == null || string.IsNullOrEmpty(resource.MimeType))
            return "fas fa-file"; // Fallback icon
        
        // Get the file extension based on MIME type
        var extension = resource.MimeType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/gif" => "gif",
            "image/svg+xml" => "svg",
        
            "audio/mpeg" => "mp3",
            "audio/wav" => "wav",
            "audio/ogg" => "ogg",
            "audio/aac" => "aac",
        
            "video/mkv" => "mkv",
            "video/mp4" => "mp4",
            "video/webm" => "webm",
            "video/ogg" => "ogg",
        
            "application/pdf" => "pdf",
            "application/zip" => "zip",
            "application/vnd.ms-excel" => "xls",
            "application/msword" => "doc",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "xlsx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "docx",
        
            "text/plain" => "txt",
            "text/csv" => "csv",
            "text/html" => "html",
        
            _ => null
        };

        // Use the helper for known extensions or fall back on Font Awesome icons
        if (extension != null)
            return IconHelper.GetImage(extension);

        return resource.MimeType switch
        {
            "image/" => "fas fa-file-image",
            "audio/" => "fas fa-file-audio",
            "video/" => "fas fa-file-video",
            "application/" => "fas fa-file-alt",
            "text/" => "fas fa-file-alt",
            _ => "fas fa-file" // General fallback icon
        };
    }
}