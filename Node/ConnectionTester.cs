// using FileFlows.RemoteServices;
// using FileFlows.ServerShared.Models;
// using FileFlows.ServerShared.Services;
//
// namespace FileFlows.Node;
//
// public class ConnectionTester
// {
//     public static (bool, string) SaveConnection(string url, string tempPath, int runners, bool enabled, List<RegisterModelMapping> mappings)
//     {
//         if (string.IsNullOrWhiteSpace(url))
//             return (false, string.Empty);
//
//         string address = url.ToLower().Replace("http://", "").Replace("https://", "");
//         int givenPort = 5151;
//         var portMatch = Regex.Match(address, @"(?<=(:))[\d]+");
//         if(portMatch is { Success: true })
//         {
//             _ = int.TryParse(portMatch.Value, out givenPort);
//         }
//         if (address.IndexOf(':') > 0)
//             address = address[..address.IndexOf(':')];
//
//         if (address.IndexOf('/') > 0)
//             address = address[..address.IndexOf('/')];
//
//         // try the common set of ports protocols
//         foreach (int port in new[] { givenPort, 19200, 5000, 5151, 80 }.Distinct())
//         {
//             if (port < 0 || port > 65535)
//                 continue;
//
//             foreach (string protocol in new[] { "https", "http" })
//             {
//                 try
//                 {
//                     var nodeService = ServiceLoader.Load<INodeService>();
//                     string actualUrl = protocol + "://" + address + ":" + port + "/";
//                     var result = nodeService.Register(actualUrl, Environment.MachineName, tempPath, mappings, null).Result;
//                     if (result == null)
//                         return (false, "Failed to register");
//                     return (true, actualUrl);
//
//                 }
//                 catch (Exception)
//                 {
//                 }
//             }
//         }
//
//         return (false, "Failed to register");
//     }
// }
