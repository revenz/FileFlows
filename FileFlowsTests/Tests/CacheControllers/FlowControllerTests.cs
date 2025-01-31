// using FileFlows.WebServer.Controllers;
// using MySqlConnector;
//
// namespace FileFlowsTests.Tests.CacheControllers;
//
// /// <summary>
// /// Test for the flow controller
// /// </summary>
// [TestClass]
// public class FlowControllerTests:CacheControllerTestBase
// {
//     /// <summary>
//     /// Tests Adding a new library
//     /// </summary>
//     [TestMethod]
//     public void Add()
//     {
//         var controller = new FlowController();
//         string name = Guid.NewGuid().ToString();
//         Flow flow = new();
//         flow.Name = name;
//         flow.Enabled = true;
//         flow.Parts = new List<FlowPart>()
//         {
//             new()
//             {
//                 Name = string.Empty,
//                 Uid = Guid.NewGuid(),
//                 FlowElementUid = "FileFlows.BasicNodes.File.InputFile",
//                 xPos = 450,
//                 yPos = 50,
//                 Icon = "far fa-file",
//                 Inputs = 0,
//                 Outputs = 1,
//                 Type = 0,
//             }
//         };
//         var updated = controller.Save(flow).Result;
//         var list = controller.GetAll().Result;
//         var fromList = list.First(x => x.Name == name);
//         foreach (var other in new[] { updated, fromList })
//         {
//             Assert.AreEqual(name, other.Name);
//         }
//     }
// }