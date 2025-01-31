using System.Dynamic;

/// <summary>
/// Provides helper methods for working with ExpandoObjects.
/// </summary>
public static class ExpandoHelper
{
    /// <summary>
    /// Converts an anonymous object into an ExpandoObject.
    /// </summary>
    /// <param name="anonymousObject">The anonymous object to convert.</param>
    /// <returns>
    /// An ExpandoObject containing the properties and values of the given anonymous object.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="anonymousObject"/> is null.</exception>
    public static ExpandoObject ToExpandoObject(object anonymousObject)
    {
        if (anonymousObject == null)
            throw new ArgumentNullException(nameof(anonymousObject), "The input object cannot be null.");

        var expando = new ExpandoObject();
        var dictionary = (IDictionary<string, object?>)expando;

        // Use reflection to get all properties of the anonymous object
        foreach (var property in anonymousObject.GetType().GetProperties())
        {
            // Add each property name and value to the ExpandoObject dictionary
            dictionary[property.Name] = property.GetValue(anonymousObject);
        }

        return expando;
    }
}