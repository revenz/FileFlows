using Jeffijoe.MessageFormat;
using System.Text.Json;
using System.Dynamic;
using System.Text.Encodings.Web;

namespace FileFlows.Shared;

/// <summary>
/// Translater intance 
/// </summary>
public class TranslaterInstance
{
    /// <summary>
    /// Formatter for formatting messages.
    /// </summary>
    private MessageFormatter Formatter;

    /// <summary>
    /// Dictionary containing language translations.
    /// </summary>
    private Dictionary<string, string> Language { get; set; } = new ();

    /// <summary>
    /// Regular expression to check if a string needs translating.
    /// </summary>
    private Regex rgxNeedsTranslating = new (@"^([\w\d_\-]+\.)+[\w\d_\-]+$");


    /// <summary>
    /// Translates a string if the string needs translating
    /// </summary>
    /// <param name="value">The string to translate if needed</param>
    /// <returns>The translated string if needed, otherwise the original string</returns>
    public string TranslateIfNeeded(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        if (NeedsTranslating(value) == false)
            return value;
        return Instant(value);
    }
    
    /// <summary>
    /// Gets the keys
    /// </summary>
    public List<string> GetKeys() => Language.Keys.ToList();

    /// <summary>
    /// Gets if the translator has been initialized
    /// </summary>
    public bool InitDone => Formatter != null;

    /// <summary>
    /// Checks if a string needs translating
    /// </summary>
    /// <param name="label">The string to test</param>
    /// <returns>if the string needs to be translated or not</returns>
    public bool NeedsTranslating(string label) => rgxNeedsTranslating.IsMatch(label ?? "");
    
    /// <summary>
    /// Initializes the translator
    /// </summary>
    /// <param name="json">the language JSON</param>
    public TranslaterInstance(string json)
    {
        Formatter ??= new MessageFormatter();
        Language = Translater.DeserializeAndFlatten(json);
    }
    
    /// <summary>
    /// Looks up a translation for a list of possible keys.
    /// </summary>
    /// <param name="possibleKeys">The possible keys to look up</param>
    /// <param name="supressWarnings">If warnings should be suppressed</param>
    /// <returns>The translation if found, otherwise the first key</returns>
    private string Lookup(string[] possibleKeys, bool supressWarnings = false)
    {
        foreach (var key in possibleKeys)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;
            if (Language.TryGetValue(key, out var lookup))
                return lookup;
        }
        if (possibleKeys[0].EndsWith("-Help") || possibleKeys[0].EndsWith("-Placeholder") || possibleKeys[0].EndsWith("-Suffix") || possibleKeys[0].EndsWith("-Prefix") || possibleKeys[0].EndsWith(".Description"))
            return "";

        if (possibleKeys[0].EndsWith(".Name") && Language.ContainsKey("Labels.Name"))
            return Language["Labels.Name"];

        string result = possibleKeys?.FirstOrDefault() ?? "";
        if(supressWarnings == false && result.EndsWith(".UID") == false && result.StartsWith("Flow.Parts.") == false)
            Logger.Instance.WLog("Failed to lookup key: " + result);
        result = result[(result.LastIndexOf(".", StringComparison.Ordinal) + 1)..];

        return result;
    }

    /// <summary>
    /// Translates a string
    /// </summary>
    /// <param name="key">The string to translate</param>
    /// <param name="parameters">any translation parameters</param>
    /// <param name="suppressWarnings">if translation warnings should be suppressed and not printed to the log</param>
    /// <returns>the translated string</returns>
    public string Instant(string key, object parameters = null, bool suppressWarnings = false)
        => Instant(new[] { key }, parameters, suppressWarnings: suppressWarnings);

    /// <summary>
    /// Attempts to translate from a range of possible keys.
    /// The first key found in the translation dictionary will be returned
    /// </summary>
    /// <param name="possibleKeys">a list of possible translation keys</param>
    /// <param name="parameters">any translation parameters</param>
    /// <param name="suppressWarnings">if translation warnings should be suppressed and not printed to the log</param>
    /// <returns>the translated string</returns>
    public string Instant(string[] possibleKeys, object parameters = null, bool suppressWarnings = false)
    {
        try
        {
            string msg = Lookup(possibleKeys, supressWarnings: suppressWarnings);
            if (msg == "")
                return "";
            if (parameters is IDictionary<string, object> dict)
                return Formatter.FormatMessage(msg, dict);

            return Formatter.FormatMessage(msg, parameters ?? new { });
        }
        catch (Exception ex)
        {
            if (possibleKeys[0].EndsWith(".UID"))
                return "UID";
            if(suppressWarnings == false)
                Logger.Instance.WLog("Failed to translating key: " + possibleKeys[0] + ", " + ex.Message);
            return possibleKeys[0];
        }
    }
}