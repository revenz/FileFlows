// using System;
// using System.IO;
// using System.IO.Pipes;
// using System.Text.Json;
// using System.Threading.Tasks;
// using FileFlows.ServerShared.Models;
// using FileFlows.Shared.Models;
//
// public static class JsonRpcClient
// {
//     private static NamedPipeClientStream client;
//     private static StreamReader reader;
//     private static StreamWriter writer;
//
//     public static async Task Initialize(string pipeName)
//     {
//         client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
//         await client.ConnectAsync();
//
//         reader = new StreamReader(client);
//         writer = new StreamWriter(client) { AutoFlush = true };
//     }
//
//     public static async Task DeleteLibraryFile(Guid uid)
//     {
//         await SendRequest("DeleteLibraryFile", uid);
//     }
//     public static async Task<LibraryFile> Get(Guid uid)
//     {
//         return await SendRequest<LibraryFile>("GetLibraryFile", uid);
//     }
//
//     public static async Task<RunnerParameters> GetRunnerParameters()
//     {
//         return await SendRequest<RunnerParameters>("GetRunnerParameters");
//     }
//
//     public static async Task<LibraryFile> GetLibraryFile(Guid uid)
//     {
//         return await SendRequest<LibraryFile>("GetLibraryFile", uid);
//     }
//
//     public static async Task<bool> ExistsOnServer(Guid uid)
//     {
//         return await SendRequest<bool>("ExistsOnServer", uid);
//     }
//
//     public static async Task<bool> SendEmail(string[] to, string subject, string body)
//     {
//         return await SendRequest<bool>("SendEmail", to, subject, body);
//     }
//
//     public static async Task Finish(FlowExecutorInfo info)
//     {
//         await SendRequest("Finish", info);
//     }
//
//     private static async Task SendRequest(string method, params object[] parameters)
//     {
//         var request = JsonSerializer.Serialize(new { Method = method, Params = parameters });
//         await writer.WriteLineAsync(request);
//     }
//
//     private static async Task<T> SendRequest<T>(string method, params object[] parameters)
//     {
//         var request = JsonSerializer.Serialize(new { Method = method, Params = parameters });
//         await writer.WriteLineAsync(request);
//
//         var responseJson = await reader.ReadLineAsync();
//         var response = JsonSerializer.Deserialize<RpcResponse<T>>(responseJson);
//         return response.Result;
//     }
// }
//
// class RpcResponse<T>
// {
//     public T Result { get; set; }
// }