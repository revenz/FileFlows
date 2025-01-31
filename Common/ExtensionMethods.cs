using System.Text;
using System.Text.RegularExpressions;

namespace FileFlows.Common;

/// <summary>
/// Extension Methods
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// Returns an empty string as null, otherwise returns the original string
    /// </summary>
    /// <param name="str">the input string</param>
    /// <returns>the string or null if empty</returns>
    public static string? EmptyAsNull(this string str)
        => str == string.Empty ? null : str;    
    
    /// <summary>
    /// Tries to find a match 
    /// </summary>
    /// <param name="regex">The regex to use to find the match</param>
    /// <param name="input">The input string to find the match in</param>
    /// <param name="match">The match if found</param>
    /// <returns>true if found, false otherwise</returns>
    public static bool TryMatch(this Regex regex, string input, out Match match)
    {
        match = regex.Match(input ?? string.Empty);
        return match.Success;
    }
    
    /// <summary>
    /// Converts an object to a json string
    /// </summary>
    /// <param name="o">the object to convert</param>
    /// <returns>the object as json, or empty if object was null</returns>
    public static string ToJson(this object o)
    {
        if (o == null)
            return "";
        return System.Text.Json.JsonSerializer.Serialize(o);
    }

    /// <summary>
    /// Lower cases UPPER case characters, but does not touch umlauts etc
    /// </summary>
    /// <param name="input">the string to lowercase</param>
    /// <returns>the lowercased string</returns>
    public static string ToLowerExplicit(this string input)
    {
        var sb = new StringBuilder(input);
        for(int i= 0; i < sb.Length; i++)   
        {
            var c = sb[i];
            if (c >= 'A' && c <= 'Z')
                sb[i] = c.ToString().ToLower()[0];
        }
        return sb.ToString();
    }
}