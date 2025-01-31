using FileHelper = FileFlows.Plugin.Helpers.FileHelper;

namespace FileFlowsTests.Tests;

/// <summary>
/// Test class for the FileHelper.ChangeExtension method.
/// </summary>
[TestClass]
public class FileHelperChangeExtensionTests
{
    /// <summary>
    /// Tests that the ChangeExtension method correctly changes the extension of a valid file name.
    /// </summary>
    [TestMethod]
    public void ChangeExtension_ValidFileNameAndExtension_ReturnsChangedFileName()
    {
        // Arrange
        string fileName = "example.txt";
        string newExtension = "csv";

        // Act
        string result = FileHelper.ChangeExtension(fileName, newExtension);

        // Assert
        Assert.AreEqual("example.csv", result);
    }

    /// <summary>
    /// Tests that the ChangeExtension method correctly changes the extension with a dot-prefixed new extension.
    /// </summary>
    [TestMethod]
    public void ChangeExtension_ValidFileNameAndDotPrefixedExtension_ReturnsChangedFileName()
    {
        // Arrange
        string fileName = "document.doc";
        string newExtension = ".pdf";

        // Act
        string result = FileHelper.ChangeExtension(fileName, newExtension);

        // Assert
        Assert.AreEqual("document.pdf", result);
    }
    
    /// <summary>
    /// Tests that the ChangeExtension method correctly changes the extension with a dot-prefixed new extension
    /// when the original file name has an extension.
    /// </summary>
    [TestMethod]
    public void ChangeExtension_OriginalFileNameWithExtension_DotPrefixedExtension_ReturnsChangedFileName2()
    {
        // Arrange
        string fileName = "example.txt";
        string newExtension = ".csv";

        // Act
        string result = FileHelper.ChangeExtension(fileName, newExtension);

        // Assert
        Assert.AreEqual("example.csv", result);
    }

    /// <summary>
    /// Tests that the ChangeExtension method correctly handles a file name without an extension.
    /// </summary>
    [TestMethod]
    public void ChangeExtension_FileNameWithoutExtension_ReturnsChangedFileName()
    {
        // Arrange
        string fileName = "fileWithoutExtension";
        string newExtension = "txt";

        // Act
        string result = FileHelper.ChangeExtension(fileName, newExtension);

        // Assert
        Assert.AreEqual("fileWithoutExtension.txt", result);
    }

    /// <summary>
    /// Tests that the ChangeExtension method correctly adds a new extension if the original file name has no extension.
    /// </summary>
    [TestMethod]
    public void ChangeExtension_FileNameWithoutExtension_AddsNewExtension()
    {
        // Arrange
        string fileName = "fileWithoutExtension";
        string newExtension = "csv";

        // Act
        string result = FileHelper.ChangeExtension(fileName, newExtension);

        // Assert
        Assert.AreEqual("fileWithoutExtension.csv", result);
    }

    /// <summary>
    /// Tests that the ChangeExtension method correctly changes the extension with a dot-prefixed new extension
    /// when the original file name has an extension.
    /// </summary>
    [TestMethod]
    public void ChangeExtension_OriginalFileNameWithExtension_DotPrefixedExtension_ReturnsChangedFileName()
    {
        // Arrange
        string fileName = "example.txt";
        string newExtension = ".csv";

        // Act
        string result = FileHelper.ChangeExtension(fileName, newExtension);

        // Assert
        Assert.AreEqual("example.csv", result);
    }
    
    /// <summary>
    /// Tests that the ChangeExtension method throws ArgumentNullException when the file name is null.
    /// </summary>
    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void ChangeExtension_NullFileName_ThrowsArgumentNullException()
    {
        // Arrange
        string? fileName = null;
        string newExtension = "csv";

        // Act
        FileHelper.ChangeExtension(fileName!, newExtension);
    }
}
