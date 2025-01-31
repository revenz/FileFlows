using System.Threading.Tasks;

namespace FileFlows.Validators;

/// <summary>
/// A validator to validate a number between a range of values
/// </summary>
public class Range : Validator
{
    /// <summary>
    /// Gets or sets the minimum valid number
    /// </summary>
    public int Minimum { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum valid number
    /// </summary>
    public int Maximum { get; set; }

    /// <summary>
    /// Validates a value against the range
    /// </summary>
    /// <param name="value">the value to validate</param>
    /// <returns>true if the value is valid, otherwise false</returns>
    public async override Task<(bool Valid, string Error)> Validate(object value)
    {
        await Task.CompletedTask;

        if (value is Int64 i64)
            return (i64 >= Minimum && i64 <= Maximum, string.Empty);
        if (value is Int32 i32)
            return (i32 >= Minimum && i32 <= Maximum, string.Empty);
        return (true, string.Empty);
    }
}