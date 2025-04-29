using System.ComponentModel.DataAnnotations;

namespace FileFlows.Shared.Models;

/// <summary>
/// Webhook for FileFlows
/// </summary>
public class Webhook: Script
{
    /// <summary>
    /// Gets or sets the route of this webhook
    /// </summary>
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9\-._+]+$")]
    public string Route { get; set; }
    
    /// <summary>
    /// Gets or sets the HTTP Method
    /// </summary>
    public HttpMethod Method { get; set; }

    /// <summary>
    /// Checks if a webhook has changed
    /// </summary>
    /// <param name="webhook">the webhook to compare against</param>
    /// <returns>true if changed, otherwise false</returns>
    public bool HasChanged(Webhook webhook)
    {
        if (webhook.Name != this.Name)
            return true;
        if (webhook.Route != this.Route)
            return true;
        if (webhook.Method != this.Method)
            return true;

        string thisCode = RemoveComments(this.Code);
        string otherCode = RemoveComments(webhook.Code);

        return CodeBlocksChanged(thisCode, otherCode);
    }

    /// <summary>
    /// Removes top level comments from the code
    /// </summary>
    /// <param name="code">the code</param>
    /// <returns>just the actual code</returns>
    private string RemoveComments(string code)
    {
        if(code.StartsWith("// path: "))
            code = code.Substring(code.IndexOf('\n') + 1).Trim();
        Regex regex = new Regex(@"^/\*[^*]*\*+(?:[^/*][^*]*\*+)*/");
        code = regex.Replace(code, string.Empty).Trim();
        return code;
    }
    
    /// <summary>
    /// Compares if two code blocks changed
    /// </summary>
    /// <param name="block1">the first code block</param>
    /// <param name="block2">the second code block</param>
    /// <returns>true if the changed, otherwise false</returns>
    public static bool CodeBlocksChanged(string block1, string block2)
    {
        // Remove all whitespace characters from both blocks
        string strippedBlock1 = Regex.Replace(block1, @"\s+", string.Empty);
        string strippedBlock2 = Regex.Replace(block2, @"\s+", string.Empty);

        // If the stripped blocks are equal, return false (no change)
        if (strippedBlock1 == strippedBlock2)
            return false;

        // Replace all variable names and other identifiers in both blocks with a common placeholder
        string placeholder = "###ID###";
        string block1WithoutIds = Regex.Replace(strippedBlock1, @"\b\w+\b", placeholder);
        string block2WithoutIds = Regex.Replace(strippedBlock2, @"\b\w+\b", placeholder);

        // If the stripped blocks without identifiers are equal, return false (no change)
        if (block1WithoutIds == block2WithoutIds)
            return false;

        // Replace all operators and keywords with a common placeholder
        string opPlaceholder = "###OP###";
        string keywordPlaceholder = "###KW###";
        const string idPattern = @"(\+|\-|\*|\/|\%|\=|\&|\||\^|\!|\~|\>|\<|\?|\:|\;|\,)";
        const string keywordPattern = @"\b(if|else|while|do|for|switch|case|break|continue|return|throw|try|catch|finally|new|typeof|void|delete|instanceof|this|true|false|null)\b";
        string block1WithoutOps = Regex.Replace(block1WithoutIds, idPattern, opPlaceholder);
        block1WithoutOps = Regex.Replace(block1WithoutOps, keywordPattern, keywordPlaceholder);
        string block2WithoutOps = Regex.Replace(block2WithoutIds, idPattern, opPlaceholder);
        block2WithoutOps = Regex.Replace(block2WithoutOps, keywordPattern, keywordPlaceholder);

        // If the stripped blocks without operators and keywords are equal, return false (no change)
        if (block1WithoutOps == block2WithoutOps)
            return false;

        // Otherwise, return true (change detected)
        return true;
    }
}