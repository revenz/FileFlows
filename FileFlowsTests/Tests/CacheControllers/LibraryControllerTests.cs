// using FileFlows.WebServer.Controllers;
// using MySqlConnector;
//
// namespace FileFlowsTests.Tests.CacheControllers;
//
// /// <summary>
// /// Test for the library controller
// /// </summary>
// [TestClass]
// public class LibraryControllerTests:CacheControllerTestBase
// {
//     private Flow CreateFlow()
//     {
//         Flow flow = new();
//         flow.Name = Guid.NewGuid().ToString();
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
//         return new FileFlows.Services.FlowService().Update(flow, null).Result;
//     }
//     
//     // /// <summary>
//     // /// Tests Adding a new library
//     // /// </summary>
//     // [TestMethod]
//     // public void Add()
//     // {
//     //     var flow = CreateFlow();
//     //     var controller = new LibraryController();
//     //     string name = Guid.NewGuid().ToString();
//     //     string path = "/test/path";
//     //     var priority = ProcessingPriority.Low;
//     //     var order = ProcessingOrder.LargestFirst;
//     //     int holdMins = 123;
//     //     var filter = "filter-this";
//     //     var exclude = "exclude-this";
//     //     var lib = new Library()
//     //     {
//     //         Flow = new ()
//     //         {
//     //             Name = flow.Name,
//     //             Type = flow.GetType().FullName,
//     //             Uid = flow.Uid
//     //         },
//     //         Name = name,
//     //         Path = path,
//     //         Priority = priority,
//     //         ProcessingOrder = order,
//     //         HoldMinutes = holdMins,
//     //         Enabled = true,
//     //         Schedule = new string('1', 672),
//     //         Filter = filter,
//     //         ExclusionFilter = exclude
//     //     };
//     //     var updated = controller.Save(lib).Result;
//     //     var list = controller.GetAll().Result;
//     //     var fromList = list.First(x => x.Name == name);
//     //     foreach (var other in new[] { updated, fromList })
//     //     {
//     //         Assert.AreEqual(name, other.Name);
//     //         Assert.AreEqual(path, other.Path);
//     //         Assert.AreEqual(priority, other.Priority);
//     //         Assert.AreEqual(order, other.ProcessingOrder);
//     //         Assert.AreEqual(holdMins, other.HoldMinutes);
//     //         Assert.AreEqual(filter, other.Filter);
//     //         Assert.AreEqual(exclude, other.ExclusionFilter);
//     //     }
//     // }
// }