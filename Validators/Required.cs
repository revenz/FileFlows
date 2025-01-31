using System.Collections;
using System.Threading.Tasks;

namespace FileFlows.Validators;

/// <summary>
/// Validator to a validate a object has a value
/// </summary>
public class Required : Validator
{
    /// <summary>
    /// Validates the object has a value
    /// </summary>
    /// <param name="value">The value to validate</param>
    /// <returns>true if the value has a propre value</returns>
    public async override Task<(bool Valid, string Error)> Validate(object value)
    {
        await Task.CompletedTask;
        if (value == null)
            return (false, string.Empty);
        if (value is string str)
        {
            bool valid = string.IsNullOrWhiteSpace(str) == false;
            return (valid, string.Empty);

        }

        if (value is Array array)
            return (array.Length > 0, string.Empty);

        if (value is ICollection collection)
            return (collection.Count > 0, string.Empty);

        return (true, string.Empty);
    }
}