using FileFlows.Client.Components;
using FileFlows.Client.Components.Inputs;
using FileFlows.Client.Services.Frontend;
using FileFlows.Plugin;
using Microsoft.JSInterop;

namespace FileFlows.Client.Helpers;

public class LibraryFileEditor
{
    static string ApIUrl => "/api/library-file";

    private static List<Tag> Tags = [];

    private static async Task<RequestResult<LibraryFileModel>> GetLibraryFile(string url)
    {
        return await HttpHelper.Get<LibraryFileModel>(url);
    }
    private static async Task<RequestResult<string>> GetLibraryFileLog(string url)
    {
        return await HttpHelper.Get<string>(url);
    }

    public static async Task Open(Blocker blocker, Editor editor, Guid libraryItemUid, Profile profile, FrontendService feService)
    {
        LibraryFileModel? model = null;
        Tags = feService.Tag.Tags;
        foreach(var tag in Tags)
            Logger.Instance.ILog("Known Tag: " + tag.Name);
        string logUrl = ApIUrl + "/" + libraryItemUid + "/log";
        blocker.Show();
        try
        {
            var result = await GetLibraryFile(ApIUrl + "/" + libraryItemUid);
            if (result.Success == false)
            {
                Toast.ShowError(
                    result.Success || string.IsNullOrEmpty(result.Body) ? Translater.Instant("ErrorMessage.NotFound") : Translater.TranslateIfNeeded(result.Body),
                    duration: 60_000
                );
                return;
            }


            model = result.Data;
            if (model.Status == FileStatus.Processing)
                logUrl += "?lines=5000";

            var logResult = await GetLibraryFileLog(logUrl);
            model.Log = (logResult.Success ? logResult.Data : string.Empty) ?? string.Empty;

            if (model.Tags?.Any() == true)
            {
                foreach(var tag in model.Tags)
                    Logger.Instance.ILog("Model Tag: " + tag);
            }
            

        }
        finally
        {

            blocker.Hide();
        }


        if(new[] { FileStatus.Unprocessed, FileStatus.Disabled, FileStatus.OutOfSchedule }.Contains(model.Status) == false)
        {
            // show tabs
            var tabs = new Dictionary<string, List<IFlowField>>();

            tabs.Add("Info", GetInfoTab(model));

            tabs.Add("Log", new List<IFlowField>
            {
                new ElementField()
                {
                    InputType = FormInputType.LogView,
                    Name = "Log",
                    Parameters = model.Status == FileStatus.Processing ? new Dictionary<string, object> {
                        { nameof(InputLogView.RefreshUrl), logUrl },
                        { nameof(InputLogView.RefreshSeconds), 5 },
                    } : null
                }
            });

            if (model.OriginalMetadata?.Any() == true)
            {
                tabs.Add("Pages.LibraryFile.Tabs.OriginalMetadata", new List<IFlowField>
                {
                    new ElementField()
                    {
                        InputType = FormInputType.Metadata,
                        Name = nameof(model.OriginalMetadata)
                    }
                });
            }
            if (model.FinalMetadata?.Any() == true)
            {
                tabs.Add("Pages.LibraryFile.Tabs.FinalMetadata", new List<IFlowField>
                {
                    new ElementField()
                    {
                        InputType = FormInputType.Metadata,
                        Name = nameof(model.FinalMetadata)
                    }
                });
            }

            if (model.CustomVariables?.Any() == true)
            {
                tabs.Add("Pages.LibraryFile.Tabs.CustomVariables", new List<IFlowField>
                {
                    new ElementField()
                    {
                        InputType = FormInputType.KeyValue,
                        Name = nameof(model.CustomVariables)
                    }
                });
            }

            // if (model.Additional?.ExecutedFlows?.Any() == true)
            // {
            //     tabs.Add("Pages.LibraryFile.Tabs.ExecutedFlows", new List<IFlowField>
            //     {
            //         new ElementField()
            //         {
            //             InputType = FormInputType.Flow,
            //             Name = nameof(model.Additional.ExecutedFlows)
            //         }
            //     });
            //
            // }
            // else 
            if (model.ExecutedNodes?.Any() == true)
            {
                tabs.Add("Pages.LibraryFile.Fields.ExecutedNodes", new List<IFlowField>
                {
                    new ElementField()
                    {
                        InputType = FormInputType.ExecutedFlowElementsRenderer,
                        Name = nameof(model.ExecutedNodes),
                        Parameters =
                        {
                            // { "Log", model.Log }
                        }
                    }
                });
            }

            var additionalButtons = new ActionButton[]
            {
                model.Status is FileStatus.Processed 
                    or FileStatus.ProcessingFailed or FileStatus.FlowNotFound
                    or FileStatus.ReprocessByFlow
                    ? new()
                    {
                        Uid = "download-log",
                        Label = App.Instance.IsMobile ? "Labels.DownloadLogShort" : "Labels.DownloadLog",
                        Clicked = (sender, e) => _ = DownloadLog(sender, libraryItemUid)
                    }
                    : null,
                model.Status == FileStatus.ProcessingFailed
                    ? new()
                    {
                        Label = "Pages.LibraryFiles.Buttons.Reprocess",
                        Clicked = (sender, e) => _ = Reprocess(sender, libraryItemUid)
                    }
                    : null
            }.Where(x => x != null).ToArray();
            if (App.Instance.IsMobile)
            {
                await editor.Open(new()
                {
                    TypeName = "Pages.LibraryFile", Title = model.RelativePath, Model = model, Tabs = tabs,
                    Large = true, ReadOnly = true, NoTranslateTitle = true, AdditionalButtons = additionalButtons 
                });
            }
            else
            {
                await editor.Open(new()
                {
                    TypeName = "Pages.LibraryFile", Title = model.RelativePath, Model = model, Tabs = tabs,
                    Large = true, ReadOnly = true, NoTranslateTitle = true, AdditionalButtons = additionalButtons
                });
                
            }
        }
        else
        {
            // just show basic info
            await editor.Open(new () { TypeName = "Pages.LibraryFile", Title = model.RelativePath, Fields = GetInfoTab(model), Model = model, Large = true, ReadOnly = true, NoTranslateTitle = true});
        }
    }

    /// <summary>
    /// Downloads the library files log file
    /// </summary>
    /// <param name="sender">the sender</param>
    /// <param name="uid">the UID of the library file</param>
    private static async Task DownloadLog(object sender, Guid uid)
    {
        if (sender is Editor editor == false)
            return;
        string downloadUrl = $"{ApIUrl}/{uid}/log/download";
#if(DEBUG)
        downloadUrl = "http://localhost:6868" + downloadUrl;
#endif
        var result = await HttpHelper.Get<string>(downloadUrl);
        if (result.Success == false)
        {
            Toast.ShowEditorError(Translater.Instant("Pages.LibraryFiles.Messages.FailedToDownloadLog"));
            return;
        }

        await editor.jsRuntime.InvokeVoidAsync("ff.saveTextAsFile", $"{uid}.log", result.Body);
    }

    /// <summary>
    /// Reprocess the file
    /// </summary>
    /// <param name="sender">the sender</param>
    /// <param name="uid">the UID of the library file</param>
    private static async Task Reprocess(object sender, Guid uid)
    {
        if (sender is Editor editor == false)
            return;
        
        string url = $"{ApIUrl}/reprocess";
#if(DEBUG)
        url = "http://localhost:6868" + url;
#endif
        var result = await HttpHelper.Post(url, new { Uids = new[] { uid } });
        if (result.Success == false)
        {
            var msg = result.Body?.EmptyAsNull() ?? Translater.Instant("Pages.LibraryFiles.Messages.FailedToReprocess");
            Toast.ShowEditorError(msg);
            return;
        }
        Toast.ShowEditorSuccess(Translater.Instant("Pages.LibraryFiles.Messages.ReprocessingFile"));
        
        await editor.Closed();
        
    }

    private static List<IFlowField> GetInfoTab(LibraryFileModel item)
    {
        List<IFlowField> fields = new ();

        fields.Add(new ElementField
        {
            InputType = FormInputType.TextLabel,
            Name = nameof(item.Name)
        });

        if (item.Name != item.OutputPath && string.IsNullOrEmpty(item.OutputPath) == false)
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.TextLabel,
                Name = nameof(item.OutputPath)
            });
        }
        fields.Add(new ElementField
        {
            InputType = FormInputType.TextLabel,
            Name = nameof(item.CreationTime)
        });

        fields.Add(new ElementField
        {
            InputType = FormInputType.TextLabel,
            Name = nameof(item.OriginalSize),
            Parameters = new Dictionary<string, object>
            {
                { nameof(InputTextLabel.Formatter), nameof(FileSizeFormatter)  }
            }
        });

        if (item.Status == FileStatus.Processed)
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.TextLabel,
                Name = nameof(item.FinalSize),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputTextLabel.Formatter), nameof(FileSizeFormatter) }
                }
            });
        }

        if (string.IsNullOrEmpty(item.Fingerprint) == false)
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.TextLabel,
                Name = nameof(item.Fingerprint)
            });
        }

        if (item.Status != FileStatus.Disabled && item.Status != FileStatus.Unprocessed &&
            item.Status != FileStatus.OutOfSchedule)
        {
            if(item.Node?.Name == "FileFlowsServer")
                item.Node.Name = Translater.Instant("Labels.InternalProcessingNode");
            fields.Add(new ElementField
            {
                InputType = FormInputType.TextLabel,
                Name = nameof(item.Node)
            });
        }
        
        if (string.IsNullOrEmpty(item.Flow?.Name) == false)
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.TextLabel,
                Name = nameof(item.Flow)
            });
        }

        if (string.IsNullOrEmpty(item.Library?.Name) == false)
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.TextLabel,
                Name = nameof(item.Library)
            });
        }

        if (item.ProcessingTime.TotalMilliseconds > 0)
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.TextLabel,
                Name = nameof(item.ProcessingTime)
            });
        }

        fields.Add(new ElementField
        {
            InputType = FormInputType.TextLabel,
            Name = nameof(item.Status),
            ReadOnlyValue = Translater.Instant("Enums.FileStatus." + item.Status)
        });

        if (item.Tags?.Any() == true && Tags?.Any() == true)
        {
            var known = Tags.Where(x => item.Tags.Contains(x.Uid)).Select(x => x.Name)
                .OrderBy(x => x.ToLowerInvariant())
                .ToList();
            if (known.Count > 0)
            {
                fields.Add(new ElementField()
                {
                    InputType = FormInputType.TextLabel,
                    Name = nameof(item.Tags),
                    ReadOnlyValue = string.Join(", ", known)
                });
            }
        }

        if (string.IsNullOrWhiteSpace(item.FailureReason) == false && item.Status == FileStatus.ProcessingFailed)
        {
            fields.Add(new ElementField
            {
                InputType = FormInputType.TextLabel,
                Name = nameof(item.FailureReason),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputTextLabel.Error), true }
                }
            });
        }

        if(item.ExecutedNodes?.Any() == true)
        {
            var flowParts = new ElementField
            {
                InputType = FormInputType.ExecutedNodes,
                Name = nameof(item.ExecutedNodes),
                Parameters = new Dictionary<string, object>
                {
                    { nameof(InputExecutedNodes.HideLabel), true },
                }
            };
            //if(item.Status != FileStatus.Processing)
                flowParts.Parameters.Add(nameof(InputExecutedNodes.Log), item.Log);
            fields.Add(flowParts);
        }

        return fields;
    }
}


public class LibraryFileModel : LibraryFile
{
    public string Log { get; set; }
}