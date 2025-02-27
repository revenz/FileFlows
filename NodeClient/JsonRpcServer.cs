// using System.IO.Pipes;
// using System.Text.Json;
// using FileFlows.ServerShared.Models;
// using FileFlows.Shared.Models;
//
// namespace FileFlows.NodeClient;
//
// class JsonRpcServer : IDisposable
// {
//     private readonly string pipeName;
//     private readonly Guid libraryFileUid;
//     private readonly RunnerParameters runnerParameters;
//     private CancellationTokenSource cts;
//     private Task serverTask;
//
//     public JsonRpcServer(RunnerParameters runnerParameters)
//     {
//         this.runnerParameters = runnerParameters;
//         this.libraryFileUid = runnerParameters.LibraryFile;
//         this.pipeName = "runner-" + libraryFileUid;
//         this.cts = new CancellationTokenSource();
//     }
//
//     public void Start()
//     {
//         serverTask = Task.Run(async () =>
//         {
//             while (!cts.Token.IsCancellationRequested)
//             {
//                 using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message);
//                 Console.WriteLine("Waiting for child process...");
//
//                 try
//                 {
//                     await server.WaitForConnectionAsync(cts.Token);
//                 }
//                 catch (OperationCanceledException)
//                 {
//                     Console.WriteLine("Server shutdown.");
//                     break;
//                 }
//
//                 Console.WriteLine("Child connected.");
//                 using var reader = new StreamReader(server);
//                 using var writer = new StreamWriter(server) { AutoFlush = true };
//
//                 while (server.IsConnected && !cts.Token.IsCancellationRequested)
//                 {
//                     var requestJson = await reader.ReadLineAsync();
//                     if (requestJson == null) break;
//
//                     var responseJson = HandleRequest(requestJson);
//                     await writer.WriteLineAsync(responseJson);
//                 }
//             }
//         }, cts.Token);
//     }
//
//     public void Stop()
//     {
//         if (cts == null) return;
//
//         Console.WriteLine("Stopping server...");
//         cts.Cancel();
//         cts.Dispose();
//         cts = null;
//     }
//
//     public void Dispose()
//     {
//         Stop();
//         serverTask?.Wait();
//         serverTask?.Dispose();
//     }
//
//
//     private string HandleRequest(string json)
//     {
//         var request = JsonSerializer.Deserialize<RpcRequest>(json);
//         object result = null;
//
//         switch (request.Method)
//         {
//             case "DeleteLibraryFile":
//                 Guid uid = Guid.Parse(request.Params[0].ToString());
//                 DeleteLibraryFile(uid);
//                 result = "Deleted";
//                 break;
//
//             case "GetRunnerParameters":
//                 result = runnerParameters;
//                 break;
//             
//             case "GetLibraryFile":
//                 uid = Guid.Parse(request.Params[0].ToString());
//                 result = GetLibraryFile(uid);
//                 break;
//
//             case "ExistsOnServer":
//                 uid = Guid.Parse(request.Params[0].ToString());
//                 result = ExistsOnServer(uid);
//                 break;
//             
//             case "SendEmail":
//                 var to = request.Params[0].ToString();
//                 var subject = request.Params[1].ToString();
//                 var body = request.Params[2].ToString();
//                 SendEmail(to, subject, body);
//                 result = true;
//                 break;
//
//             case "Finish":
//                 var info = JsonSerializer.Deserialize<FlowExecutorInfo>(request.Params[0].ToString());
//                 Finish(info);
//                 result = "Finished";
//                 break;
//
//             default:
//                 result = "Unknown method";
//                 break;
//         }
//
//         return JsonSerializer.Serialize(new { Result = result });
//     }
//
//     private void DeleteLibraryFile(Guid uid) => Console.WriteLine($"Deleted file {uid}");
//     private LibraryFile GetLibraryFile(Guid uid) => new LibraryFile { Uid = uid, Name = "Example.txt" };
//     private bool ExistsOnServer(Guid uid) => true;
//     private void Finish(FlowExecutorInfo info) => Console.WriteLine($"Flow Finished: {info.Uid}");
// }
//
// class RpcRequest
// {
//     public string Method { get; set; }
//     public object[] Params { get; set; }
// }