using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FileFlowsScriptRepo.Generators;

public class DataConverter : JsonConverter<RepositoryObject>
{
    public override RepositoryObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Write the value as JSON.
    /// </summary>
    /// <remarks>
    /// A converter may throw any Exception, but should throw <cref>JsonException</cref> when the JSON
    /// cannot be created.
    /// </remarks>
    /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write to.</param>
    /// <param name="value">The value to convert. Note that the value of determines if the converter handles values.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> being used.</param>
    public override void Write(Utf8JsonWriter writer, RepositoryObject value, JsonSerializerOptions options)
    {
        var properties = value.GetType().GetProperties();

        writer.WriteStartObject();

        foreach (var prop in properties)
        {
            var propValue = prop.GetValue(value);
            if (propValue == null)
                continue; // dont write nulls
            if (prop.PropertyType.IsPrimitive && propValue == Activator.CreateInstance(prop.PropertyType))
                continue; // dont write defaults
            if (propValue as bool? == false)
                continue; // don't write default false booleans
            if(propValue is int iValue && iValue == 0)
                continue;
            if(prop.Name == "Group")
                continue;

            writer.WritePropertyName(prop.Name);
            JsonSerializer.Serialize(writer, propValue, prop.PropertyType, options);
        }

        writer.WriteEndObject();
    }
}
