using System.IO;
using FileFlows.LibraryUtils;
using FileFlows.Server.Workers;

namespace FileFlowsTests.Tests.Libraries;


[TestClass]
public class DetectionTests : TestBase
{
    [TestMethod]
    public void FileCreation()
    {
        var lib = new Library();
        var info = new FileInfo(CreateTempFile());
        foreach(var range in Enum.GetValues<MatchRange>())
        {
            lib.DetectFileCreation = range;
            switch (range)
            {
                case MatchRange.GreaterThan:
                    lib.DetectFileCreationLower = 30; // 30 minutes 
                    info.CreationTime = DateTime.UtcNow;
                    Assert.IsFalse(Test()); 
                    info.CreationTime = DateTime.UtcNow.AddMinutes(-31);
                    Assert.IsTrue(Test());
                    break;
                case MatchRange.LessThan:
                    lib.DetectFileCreationLower = 30; // 30 minutes 
                    info.CreationTime = DateTime.UtcNow;
                    Assert.IsTrue(Test()); 
                    info.CreationTime = DateTime.UtcNow.AddMinutes(-31);
                    Assert.IsFalse(Test());
                    break;
                case MatchRange.Any:
                    lib.DetectFileCreationLower = 30; // 30 minutes 
                    info.CreationTime = DateTime.UtcNow;
                    Assert.IsTrue(Test()); 
                    info.CreationTime = DateTime.UtcNow.AddMinutes(-31);
                    Assert.IsTrue(Test());
                    break;
                case MatchRange.Between:
                    lib.DetectFileCreationLower = 30; // 30 minutes
                    lib.DetectFileCreationUpper = 60; // 60 minutes 
                    info.CreationTime = DateTime.UtcNow;
                    Assert.IsFalse(Test()); 
                    info.CreationTime = DateTime.UtcNow.AddMinutes(-31);
                    Assert.IsTrue(Test());
                    info.CreationTime = DateTime.UtcNow.AddMinutes(-59);
                    Assert.IsTrue(Test());
                    info.CreationTime = DateTime.UtcNow.AddMinutes(-61);
                    Assert.IsFalse(Test());
                    break;
                case MatchRange.NotBetween:
                    lib.DetectFileCreationLower = 30; // 30 minutes
                    lib.DetectFileCreationUpper = 60; // 60 minutes 
                    info.CreationTime = DateTime.UtcNow;
                    Assert.IsTrue(Test()); 
                    info.CreationTime = DateTime.UtcNow.AddMinutes(-31);
                    Assert.IsFalse(Test());
                    info.CreationTime = DateTime.UtcNow.AddMinutes(-59);
                    Assert.IsFalse(Test());
                    info.CreationTime = DateTime.UtcNow.AddMinutes(-61);
                    Assert.IsTrue(Test());
                    break;
            } 
        }

        bool Test()
        {
            return LibraryMatches.MatchesDetection(lib, info, 100_000);
        }
    }
    
    
    [TestMethod]
    public void FileLastWritten()
    {
        var lib = new Library();
        var info = new FileInfo(CreateTempFile());
        foreach(var range in Enum.GetValues<MatchRange>())
        {
            lib.DetectFileLastWritten = range;
            switch (range)
            {
                case MatchRange.GreaterThan:
                    lib.DetectFileLastWrittenLower = 30; // 30 minutes 
                    info.LastWriteTime = DateTime.UtcNow;
                    Assert.IsFalse(Test()); 
                    info.LastWriteTime = DateTime.UtcNow.AddMinutes(-31);
                    Assert.IsTrue(Test());
                    break;
                case MatchRange.LessThan:
                    lib.DetectFileLastWrittenLower = 30; // 30 minutes 
                    info.LastWriteTime = DateTime.UtcNow;
                    Assert.IsTrue(Test()); 
                    info.LastWriteTime = DateTime.UtcNow.AddMinutes(-31);
                    Assert.IsFalse(Test());
                    break;
                case MatchRange.Any:
                    lib.DetectFileLastWrittenLower = 30; // 30 minutes 
                    info.LastWriteTime = DateTime.UtcNow;
                    Assert.IsTrue(Test()); 
                    info.LastWriteTime = DateTime.UtcNow.AddMinutes(-31);
                    Assert.IsTrue(Test());
                    break;
                case MatchRange.Between:
                    lib.DetectFileLastWrittenLower = 30; // 30 minutes
                    lib.DetectFileLastWrittenUpper = 60; // 60 minutes 
                    info.LastWriteTime = DateTime.UtcNow;
                    Assert.IsFalse(Test()); 
                    info.LastWriteTime = DateTime.UtcNow.AddMinutes(-31);
                    Assert.IsTrue(Test());
                    info.LastWriteTime = DateTime.UtcNow.AddMinutes(-59);
                    Assert.IsTrue(Test());
                    info.LastWriteTime = DateTime.UtcNow.AddMinutes(-61);
                    Assert.IsFalse(Test());
                    break;
                case MatchRange.NotBetween:
                    lib.DetectFileLastWrittenLower = 30; // 30 minutes
                    lib.DetectFileLastWrittenUpper = 60; // 60 minutes 
                    info.LastWriteTime = DateTime.UtcNow;
                    Assert.IsTrue(Test()); 
                    info.LastWriteTime = DateTime.UtcNow.AddMinutes(-31);
                    Assert.IsFalse(Test());
                    info.LastWriteTime = DateTime.UtcNow.AddMinutes(-59);
                    Assert.IsFalse(Test());
                    info.LastWriteTime = DateTime.UtcNow.AddMinutes(-61);
                    Assert.IsTrue(Test());
                    break;
            } 
        }

        bool Test()
        {
            return LibraryMatches.MatchesDetection(lib, info, 100_000);
        }
    }
    
    
    
    [TestMethod]
    public void FileSize()
    {
        var lib = new Library();
        var info = new FileInfo(GetTempFileName());
        foreach(var range in Enum.GetValues<MatchRange>())
        {
            lib.DetectFileSize = range;
            lib.DetectFileSizeLower = 100;
            lib.DetectFileSizeUpper = 500;
            switch (range)
            {
                case MatchRange.GreaterThan:
                    Assert.IsFalse(Test(99)); 
                    Assert.IsTrue(Test(101));
                    Assert.IsFalse(Test(100)); // 100 is NOT greater than the limit
                    break;
                case MatchRange.LessThan:
                    Assert.IsTrue(Test(99)); 
                    Assert.IsFalse(Test(101));
                    Assert.IsFalse(Test(100)); // 100 is NOT less than the limit
                    break;
                case MatchRange.Any:
                    Assert.IsTrue(Test(99)); 
                    Assert.IsTrue(Test(101));
                    break;
                case MatchRange.Between:
                    Assert.IsFalse(Test(99)); 
                    Assert.IsTrue(Test(101)); 
                    Assert.IsTrue(Test(499));
                    Assert.IsFalse(Test(501));
                    Assert.IsTrue(Test(100)); // 100 is between 100 and 500
                    Assert.IsTrue(Test(500)); // 500 is between 100 and 500
                    break;
                case MatchRange.NotBetween:
                    Assert.IsTrue(Test(99)); 
                    Assert.IsFalse(Test(101)); 
                    Assert.IsFalse(Test(499));
                    Assert.IsTrue(Test(501));
                    Assert.IsFalse(Test(100)); // 100 is between 100 and 500
                    Assert.IsFalse(Test(500)); // 500 is between 100 and 500
                    break;
            } 
        }

        bool Test(long size)
        {
            return LibraryMatches.MatchesDetection(lib, info, size);
        }
    }
}