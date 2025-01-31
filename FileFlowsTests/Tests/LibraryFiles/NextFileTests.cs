// namespace FileFlowsTests.Tests.LibraryFiles;
//
// [TestClass]
// public class NextFileTests:LibraryFileTest
// {
//     [TestMethod]
//     public void Basic()
//     {
//         var service = new FileFlows.Services.LibraryFileService();
//         var nextResult = service.GetNext(CommonVariables.InternalNodeName, 
//             CommonVariables.InternalNodeUid, Globals.Version.ToString(), Guid.NewGuid()).Result;
//         Assert.IsNotNull(nextResult);
//         Assert.AreEqual(NextLibraryFileStatus.Success, nextResult.Status);
//         Assert.IsNotNull(nextResult.File);
//     }
//     
//     [TestMethod]
//     public void NodeOnlyOneLibrary()
//     {
//         var lib = Libraries[3];
//         InternalNode.Libraries = new List<ObjectReference>
//         {
//             new () { Uid = lib.Uid, Name = lib.Name, Type = lib.GetType().FullName }
//         };
//         InternalNode.AllLibraries = ProcessingLibraries.Only;
//         
//         var service = new FileFlows.Services.LibraryFileService();
//         var nextResult = service.GetNext(CommonVariables.InternalNodeName, 
//             CommonVariables.InternalNodeUid, Globals.Version.ToString(), Guid.NewGuid()).Result;
//         Assert.IsNotNull(nextResult);
//         Assert.AreEqual(NextLibraryFileStatus.Success, nextResult.Status);
//         Assert.IsNotNull(nextResult.File);
//         var next = nextResult.File;
//         Assert.AreEqual(lib.Name, next.LibraryName);
//         Assert.AreEqual(lib.Uid, next.LibraryUid);
//     }
//     
//     [TestMethod]
//     public void NodeAllExcept()
//     {
//         var lib = Libraries[3];
//         InternalNode.Libraries = Libraries.Where(x => x != lib).Select(
//             x => new ObjectReference() { Uid = x.Uid, Name = x.Name, Type = x.GetType().FullName }
//         ).ToList();
//         InternalNode.AllLibraries = ProcessingLibraries.AllExcept;
//         
//         var service = new FileFlows.Services.LibraryFileService();
//         var nextResult = service.GetNext(CommonVariables.InternalNodeName, 
//             CommonVariables.InternalNodeUid, Globals.Version.ToString(), Guid.NewGuid()).Result;
//         Assert.IsNotNull(nextResult);
//         Assert.AreEqual(NextLibraryFileStatus.Success, nextResult.Status);
//         Assert.IsNotNull(nextResult.File);
//         var next = nextResult.File;
//         Assert.AreEqual(lib.Name, next.LibraryName);
//         Assert.AreEqual(lib.Uid, next.LibraryUid);
//     }
//     
//     [TestMethod]
//     public void OldestFirst()
//     {
//         var lib = AllowLibrary(ProcessingOrder.OldestFirst);
//         var possibleFiles = Files.Values.Where(x => x.LibraryUid == lib.Uid && x.Status == FileStatus.Unprocessed).ToList(); 
//         var expected = possibleFiles.OrderBy(x => x.CreationTime).First();
//         ExpectFile(expected);
//     }
//     
//     [TestMethod]
//     public void NewestFirst()
//     {
//         var lib = AllowLibrary(ProcessingOrder.NewestFirst);
//         var possibleFiles = Files.Values.Where(x => x.LibraryUid == lib.Uid && x.Status == FileStatus.Unprocessed).ToList(); 
//         var expected = possibleFiles.OrderByDescending(x => x.CreationTime).First();
//         ExpectFile(expected);
//     }
//     
//     [TestMethod]
//     public void SmallestFirst()
//     {
//         var lib = AllowLibrary(ProcessingOrder.SmallestFirst);
//         var possibleFiles = Files.Values.Where(x => x.LibraryUid == lib.Uid && x.Status == FileStatus.Unprocessed).ToList(); 
//         var expected = possibleFiles.OrderBy(x => x.OriginalSize).First();
//         ExpectFile(expected);
//     }
//     
//     [TestMethod]
//     public void LargestFirst()
//     {
//         var lib = AllowLibrary(ProcessingOrder.LargestFirst);
//         var possibleFiles = Files.Values.Where(x => x.LibraryUid == lib.Uid && x.Status == FileStatus.Unprocessed).ToList(); 
//         var expected = possibleFiles.OrderByDescending(x => x.OriginalSize).First();
//         ExpectFile(expected);
//     }
//     
//     [TestMethod]
//     public void AsFound()
//     {
//         var lib = AllowLibrary(ProcessingOrder.AsFound);
//         var possibleFiles = Files.Values.Where(x => x.LibraryUid == lib.Uid && x.Status == FileStatus.Unprocessed).ToList(); 
//         var expected = possibleFiles.OrderBy(x => x.DateCreated).First();
//         ExpectFile(expected);
//     }
//     
//     [TestMethod]
//     public void Disable_Forced()
//     {
//         var lib = Libraries[3];
//         lib.Enabled = false;
//         AllowLibrary(lib);
//         var expected = Files.Values.Where(x => x.LibraryUid == lib.Uid && x.Status == FileStatus.Unprocessed)
//             .OrderBy(x => rand.Next()).First();
//         expected.Flags = LibraryFileFlags.ForceProcessing;
//         ExpectFile(expected);
//     }
//     
//     [TestMethod]
//     public void Disable()
//     {
//         var lib = Libraries[3];
//         lib.Enabled = false;
//         AllowLibrary(lib);
//         var next = GetNextFile();
//         Assert.AreEqual(NextLibraryFileStatus.NoFile, next.Status);
//         Assert.IsNull(next.File);
//     }
//     
//     [TestMethod]
//     public void OutOfSchedule()
//     {
//         var lib = Libraries[3];
//         lib.Schedule = new string('0', 672);
//         AllowLibrary(lib);
//         var next = GetNextFile();
//         Assert.AreEqual(NextLibraryFileStatus.NoFile, next.Status);
//         Assert.IsNull(next.File);
//     }
//     
//     [TestMethod]
//     public void OutOfSchedule_Forced()
//     {
//         var lib = Libraries[3];
//         lib.Schedule = new string('0', 672);
//         AllowLibrary(lib);
//         var expected = Files.Values.Where(x => x.LibraryUid == lib.Uid && x.Status == FileStatus.Unprocessed)
//             .OrderBy(x => rand.Next()).First();
//         expected.Flags = LibraryFileFlags.ForceProcessing;
//         ExpectFile(expected);
//     }
//     
//     [TestMethod]
//     public void InSchedule()
//     {
//         var lib = Libraries[3];
//         StringBuilder schedule = new StringBuilder(new string('0', 672));
//
//         int quarter = TimeHelper.GetCurrentQuarter();
//         schedule[ quarter] = '1';
//         // do next quarter as well just in case this test run rolls into next quarter
//         schedule[quarter == 671 ? 0 : quarter + 1] = '1';
//         lib.Schedule = schedule.ToString();
//         
//         AllowLibrary(lib);
//         var next = GetNextFile();
//         Assert.AreEqual(NextLibraryFileStatus.Success, next.Status);
//         Assert.IsNotNull(next.File);
//     }
//     
//     [TestMethod]
//     public void MaxFileSize()
//     {
//         var lib = new Library();
//         lib.Uid = Guid.NewGuid();
//         lib.Name = "MaxFileSize";
//         lib.Enabled = true;
//         lib.Priority = ProcessingPriority.Highest;
//         lib.ProcessingOrder = ProcessingOrder.LargestFirst;
//         lib.Schedule = new string('1', 672);
//         Libraries.Add(lib);
//         var maxSizeFile = NewFile(lib);
//         InternalNode.MaxFileSizeMb = 35;
//         maxSizeFile.OriginalSize = InternalNode.MaxFileSizeMb * 1_000_000;
//         for (int i = 0; i < 1000; i++)
//         {
//             if(i == 500)
//                 Files.Add(maxSizeFile.Uid, maxSizeFile);
//             else
//             {
//                 var file = NewFile(lib);
//                 Files.Add(file.Uid, file);
//             }
//         }
//         AllowLibrary(lib);
//         ExpectFile(maxSizeFile);
//     }
//
//     [TestMethod]
//     public void MaxFileSize_SmallestFirst()
//     {
//         var lib = new Library();
//         lib.Uid = Guid.NewGuid();
//         lib.Name = "MaxFileSize";
//         lib.Enabled = true;
//         lib.Priority = ProcessingPriority.Highest;
//         lib.ProcessingOrder = ProcessingOrder.SmallestFirst;
//         lib.Schedule = new string('1', 672);
//         Libraries.Add(lib);
//         var smallest = NewFile(lib);
//         InternalNode.MaxFileSizeMb = 35;
//         smallest.OriginalSize = 1;
//         for (int i = 0; i < 1000; i++)
//         {
//             if(i == 500)
//                 Files.Add(smallest.Uid, smallest);
//             else
//             {
//                 var file = NewFile(lib);
//                 Files.Add(file.Uid, file);
//             }
//         }
//         AllowLibrary(lib);
//         ExpectFile(smallest);
//     }
//     
//     private Library AllowLibrary(ProcessingOrder order)
//     {
//         var lib = Libraries.First(x => x.ProcessingOrder == order);
//         AllowLibrary(lib);
//         return lib;
//     }
//
//     private void AllowLibrary(Library lib)
//     {
//         InternalNode.Libraries = new List<ObjectReference>
//         {
//             new () { Uid = lib.Uid, Name = lib.Name, Type = lib.GetType().FullName }
//         };
//         InternalNode.AllLibraries = ProcessingLibraries.Only;        
//     }
//     
//     private NextLibraryFileResult GetNextFile()
//     {
//         var service = new FileFlows.Services.LibraryFileService();
//         var nextResult = service.GetNext(CommonVariables.InternalNodeName, 
//             CommonVariables.InternalNodeUid, Globals.Version.ToString(), Guid.NewGuid()).Result;
//         return nextResult;
//     }
//
//     private void ExpectFile(LibraryFile expected)
//     {
//         var nextResult = GetNextFile();
//         Assert.IsNotNull(nextResult);
//         Assert.AreEqual(NextLibraryFileStatus.Success, nextResult.Status);
//         Assert.IsNotNull(nextResult.File);
//         var next = nextResult.File;
//         Assert.AreEqual(expected.LibraryName, next.LibraryName);
//         Assert.AreEqual(expected.LibraryUid, next.LibraryUid);
//         Assert.AreEqual(expected.Name, next.Name);
//         Assert.AreEqual(expected.Uid, next.Uid);
//         
//     }
// }