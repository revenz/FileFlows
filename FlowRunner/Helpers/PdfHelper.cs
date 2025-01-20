using System.Text;
using FileFlows.Plugin.Helpers;
using UglyToad.PdfPig;

namespace FileFlows.FlowRunner.Helpers;

/// <summary>
/// PDF Helper
/// </summary>
public class PdfHelper(StringHelper stringHelper) : IPdfHelper
{
    
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
            return stringHelper.Matches(text, pdfText);
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
            foreach (var page in document.GetPages())
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