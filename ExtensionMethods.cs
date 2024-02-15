namespace FileFlowsScriptRepo;

/// <summary>
/// Extension methods
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// Returns an empty string as null, otherwise returns the original string
    /// </summary>
    /// <param name="str">the input string</param>
    /// <returns>the string or null if empty</returns>
    public static string? EmptyAsNull(this string str)
    {
        return str == string.Empty ? null : str;
    }
}