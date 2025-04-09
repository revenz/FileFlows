using Microsoft.JSInterop;
using FileFlows.Client.Components;
using FileFlows.Client.Services.Frontend;

namespace FileFlows.Client.Services
{
    public interface IClipboardService
    {
        Task CopyToClipboard(string text);
    }

    public class ClipboardService : IClipboardService
    {
        private IJSRuntime JsRuntime { get; set; }
        /// <summary>
        /// Gets or sets the frontend service
        /// </summary>
        private FrontendService feService { get; set; }

        public ClipboardService(IJSRuntime jsRuntime, FrontendService feService)
        {
            this.JsRuntime = jsRuntime;
            this.feService = feService;
        }

        /// <summary>
        /// Copies text to the clipboard and shows a toast
        /// </summary>
        /// <param name="text">th text to copy</param>
        public async Task CopyToClipboard(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            await JsRuntime.InvokeVoidAsync("ff.copyToClipboard", text);
            feService.Notifications.ShowInfo(Translater.Instant("Labels.CopiedToClipboard", new { text }));
        }
    }
}
