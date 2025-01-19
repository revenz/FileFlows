using System.Text;
using FileFlows.Plugin;
using FileFlows.Plugin.Helpers;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace FileFlows.FlowRunner.Helpers;

/// <summary>
/// PDF Helper
/// </summary>
public class PdfHelper : IPdfHelper
{
    /// <summary>
    /// The logger to use
    /// </summary>
    private readonly ILogger Logger;
    /// <summary>
    /// The node parameters
    /// </summary>
    private readonly NodeParameters NodeParameters;
    
    /// <summary>
    /// Initialises a new instance of the image helper
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="args">the node parameters</param>
    public PdfHelper(ILogger logger, NodeParameters args)
    {
        Logger = logger;
        NodeParameters = args;
    }
    
    /// <inheritdoc />
    public Result<bool> ContainsText(string pdfPath, string text)
    {
        try
        {
            var result = ExtractText(pdfPath);
            if (result.Failed(out var error))
                return Result<bool>.Fail(error);

            var pdfText = result.Value;
            return pdfText.Contains(text, StringComparison.InvariantCultureIgnoreCase);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed checking PDF contains text: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> MatchesText(string pdfPath, string text)
    {
        try
        {
            var result = ExtractText(pdfPath);
            if (result.Failed(out var error))
                return Result<bool>.Fail(error);

            var pdfText = result.Value;
            return NodeParameters.StringHelper.Matches(text, pdfText);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail("Failed checking PDF contains text: " + ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<string> ExtractText(string pdfPath)
    {
        try
        {
            using var document = PdfDocument.Open(pdfPath);
            StringBuilder completeText = new();
            foreach (Page page in document.GetPages())
            {
                var pageText = page.Text;
                if (string.IsNullOrWhiteSpace(pageText) == false)
                    completeText.AppendLine(pageText);
            }
            return completeText.ToString();
        }
        catch (Exception ex)
        {
            return Result<string>.Fail("Failed extracting text from PDF: " + ex.Message);
        }
    }
}