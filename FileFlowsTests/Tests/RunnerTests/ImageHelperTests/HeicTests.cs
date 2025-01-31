// #if(DEBUG)
// using FileFlows.FlowRunner.Helpers;
//
// namespace FileFlowsTests.Tests.RunnerTests.ImageHelperTests;
//
// /// <summary>
// /// Tests HEIC files
// /// </summary>
// [TestClass]
// public class HeicTests : TestBase
// {
//     /// <summary>
//     /// Converts a HEIC file to jpg
//     /// </summary>
//     [TestMethod]
//     public void ConvertToJpeg()
//     {
//         string file = $"{ResourcesTestFilesDir}/Images/heic1.heic";
//         var magick = new ImageMagickHelper(Logger, "convert", "identify");
//         var result = magick.ConvertImage(file, $"{TempPath}/heic.jpg", new ()
//         {
//             Quality = 75
//         });
//         if(result.Failed(out string error))
//             Assert.Fail(error);
//     }
//
//     /// <summary>
//     /// Gets the image info for a HEIC file
//     /// </summary>
//     [TestMethod]
//     public void GetInfo()
//     {
//         string file = $"{ResourcesTestFilesDir}/Images/heic1.heic";
//         var helper = new ImageHelper(Logger, "convert", "identify");
//         var result = helper.GetInfo(file);
//         if(result.Failed(out var error))
//             Assert.Fail(error);
//         var info = result.Value;
//         Assert.AreEqual("HEIC", info.Format);
//     }
// }
// #endif