using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FileFlows.Validators;

/// <summary>
/// Validates a string is a safe name
/// </summary>
public class SafeName : Validator
{
    /// <summary>
    /// Validates the value against the regular expression
    /// </summary>
    /// <param name="value">the value to validate</param>
    /// <returns>true if valid otherwise false</returns>
    public async override Task<(bool Valid, string Error)> Validate(object value)
    {
        await Task.CompletedTask;

        var regex = new Regex("^[_a-zA-Z][_a-zA-Z0-9]*$");
        return (regex.IsMatch(value as string ?? ""), string.Empty);
    }
}