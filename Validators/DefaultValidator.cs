namespace FileFlows.Validators;

/// <summary>
/// Used instead of null
/// </summary>
public class DefaultValidator : Validator
{
    /// <summary>
    /// Validates a value
    /// </summary>
    /// <param name="value">the value to validate</param>
    /// <returns>If the value is valid or not</returns>
    public async override Task<(bool Valid, string Error)> Validate(object value)
    {
        await Task.CompletedTask;
        return (true, string.Empty);
    }
}