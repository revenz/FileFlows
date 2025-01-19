namespace FileFlows.Plugin.Helpers;

/// <summary>
/// PDF Helper interface
/// </summary>
public interface IPdfHelper
{
    /// <summary>
    /// Checks if the text exists in a PDF file
    /// </summary>
    /// <param name="pdfPath">the path to the PDF</param>
    /// <param name="text">the text to check for</param>
    /// <returns>true if exists, otherwise false</returns>
    Result<bool> ContainsText(string pdfPath, string text);
    
    /// <summary>
    /// Checks if the text matches in a PDF file using the StringHelper
    /// </summary>
    /// <param name="pdfPath">the path to the PDF</param>
    /// <param name="text">the match to check for</param>
    /// <returns>true if exists, otherwise false</returns>
    Result<bool> MatchesText(string pdfPath, string text);
    
    /// <summary>
    /// Extracts the text of a PDF file
    /// </summary>
    /// <param name="pdfPath">the path to the PDF</param>
    /// <returns>the extracted text</returns>
    Result<string> ExtractText(string pdfPath);
}