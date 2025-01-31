using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FileFlows.Validators;

/// <summary>
/// Validates a string against a regular expression
/// </summary>
public class Pattern : Validator
{
    /// <summary>
    /// Gets or sets the expression to validate against
    /// </summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>
    /// Validates the value against the regular expression
    /// </summary>
    /// <param name="value">the value to validate</param>
    /// <returns>true if valid otherwise false</returns>
    public async override Task<(bool Valid, string Error)> Validate(object value)
    {
        await Task.CompletedTask;

        if (string.IsNullOrEmpty(Expression))
            return (true, string.Empty);

        var regex = new Regex(Expression);
        return (regex.IsMatch(value as string ?? ""), string.Empty);
    }
}