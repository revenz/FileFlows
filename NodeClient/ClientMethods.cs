// using FileFlows.Common;
// using FileFlows.ServerShared.Services;
// using FileFlows.Shared.Models;
// using Microsoft.AspNetCore.SignalR.Client;
//
// namespace FileFlows.NodeClient;
//
// public class ClientMethods(Client client, ILogger _logger, 
//     ConfigurationService _configurationService)
// {
//     public async Task DownloadConfiguration()
//     {
//         try
//         {
//             if (client._node == null)
//                 return;
//
//             _logger.ILog("Downloading configuration...");
//             var cfg = await client._connection.InvokeAsync<ConfigurationRevision>("GetConfiguration");
//             if (cfg == null || client._node == null)
//                 return;
//             
//             await _configurationService.SaveConfiguration(cfg, client._node);
//             _logger.ILog($"Node '{client._node.Name}' Configuration updated to {cfg.Revision}");
//         }
//         catch (Exception ex)
//         {
//             _logger.ELog($"Error downloading configuration: {ex.Message}");
//         }
//     }
//
//     public async Task SendEmail(string to, string subject, string body)
//     {
//         try
//         {
//             if (client._node == null)
//                 return;
//
//             _logger.ILog("Downloading configuration...");
//             await client._connection.InvokeAsync("SendEmail", to, subject, body);
//         }
//         catch (Exception ex)
//         {
//             _logger.ELog($"Error sending email: {ex.Message}");
//         }
// }