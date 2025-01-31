using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileFlows.Validators;

/// <summary>
/// JSON Convert for validators
/// </summary>
public class ValidatorConverter : JsonConverter<Validators.Validator>
{
    /// <summary>
    /// Tests if an object type can be converted
    /// </summary>
    /// <param name="typeToConvert">the type to test</param>
    /// <returns>true if can be converted, otherwise false</returns>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(Validators.Validator) == typeToConvert;
    }
    
    /// <summary>
    /// Read and convert the JSON to T.
    /// </summary>
    /// <remarks>
    /// A converter may throw any Exception, but should throw <cref>JsonException</cref> when the JSON is invalid.
    /// </remarks>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> to read from.</param>
    /// <param name="typeToConvert">The <see cref="Type"/> being converted.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> being used.</param>
    /// <returns>The value that was converted.</returns>
    public override Validator? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (var jsonDocument = JsonDocument.ParseValue(ref reader))
        {
            foreach (string typeProperty in new[] { "Type", "type" })
            {
                if (jsonDocument.RootElement.TryGetProperty(typeProperty, out JsonElement typeValue))
                {
                    string typeName = typeValue.GetString() ?? String.Empty;
                    var vts = ValidatorTypes;
                    if (vts.ContainsKey(typeName) == false)
                        return new DefaultValidator();
                    var type = vts[typeName];
                    return jsonDocument.Deserialize(type) as Validator;
                }
            }
        }
        return new DefaultValidator();
    }

    private Dictionary<string, Type>? _ValidatorTypes;

    private Dictionary<string, Type> ValidatorTypes
    {
        get
        {
            if (_ValidatorTypes == null)
            {
                _ValidatorTypes = typeof(Validator).Assembly.GetTypes()
                                .Where(x => x.IsAbstract == false && typeof(Validator).IsAssignableFrom(x))
                                .ToDictionary(x => x.Name, x => x);
            }
            return _ValidatorTypes;
        }
    }

    /// <summary>
    /// Write the value as JSON.
    /// </summary>
    /// <remarks>
    /// A converter may throw any Exception, but should throw <cref>JsonException</cref> when the JSON
    /// cannot be created.
    /// </remarks>
    /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write to.</param>
    /// <param name="value">The value to convert.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> being used.</param>
    public override void Write(Utf8JsonWriter writer, Validator value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        var properties = value.GetType().GetProperties();

        foreach (var prop in properties)
        {
            var propValue = prop.GetValue(value);
            writer.WritePropertyName(prop.Name);
            JsonSerializer.Serialize(writer, propValue, prop.PropertyType, options);
        }

        writer.WriteEndObject();
    }
}