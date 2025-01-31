using System.Reflection;
using System.Text.Json.Serialization;
using FileFlows.Shared.Attributes;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Helpers;


/// <summary>
/// Converts a FileFlowObject
/// </summary>
public class DataConverter : JsonConverter<FileFlowObject>
{      
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
    public override FileFlowObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
    public override void Write(Utf8JsonWriter writer, FileFlowObject value, JsonSerializerOptions options)
    {
        var properties = value.GetType().GetProperties();

        writer.WriteStartObject();

        foreach (var prop in properties)
        {
            // dont write the properties that also exist on the DbObject
            if ((prop.Name == "Uid" || prop.Name == "DateModified" || prop.Name == "DateCreated" || prop.Name == "Name"  || prop.Name == "LastScannedAgo"
                 || prop.Name == "LicenseEmail" || prop.Name == "LicenseKey" || prop.Name == "LicenseCode" || prop.Name == "LicenseFlags"
                 || prop.Name == "LicenseExpiryDateUtc" || prop.Name == "LicenseProcessingNodes"|| prop.Name == "LicenseStatus"
                 || prop.Name == "DbType" || prop.Name == "DbServer" || prop.Name == "DbName" || prop.Name == "DbUser" || prop.Name == "DbPassword") == false)
            {
                var propValue = prop.GetValue(value);
                if (propValue == null)
                    continue; // dont write nulls
                if (prop.PropertyType.IsPrimitive && propValue == Activator.CreateInstance(prop.PropertyType))
                    continue; // dont write defaults
                if (propValue as bool? == false)
                    continue; // don't write default false booleans
                
                var ignore = prop.GetCustomAttribute<DbIgnoreAttribute>();
                if (ignore != null)
                    continue;

                var encrypted = prop.GetCustomAttribute<EncryptedAttribute>();
                if (encrypted != null)
                    propValue = FileFlows.Helpers.Decrypter.Encrypt(propValue as string);

                writer.WritePropertyName(prop.Name);
                JsonSerializer.Serialize(writer, propValue, prop.PropertyType, options);
            }
        }

        writer.WriteEndObject();
    }
}

/// <summary>
/// Converts a FileFlowObject
/// </summary>
public class DataConverter<T> : JsonConverter<T>
{      
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
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
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
            
            var ignore = prop.GetCustomAttribute<DbIgnoreAttribute>();
            if (ignore != null)
                continue;

            var encrypted = prop.GetCustomAttribute<EncryptedAttribute>();
            if (encrypted != null)
                propValue = FileFlows.Helpers.Decrypter.Encrypt(propValue as string);

            writer.WritePropertyName(prop.Name);
            JsonSerializer.Serialize(writer, propValue, prop.PropertyType, options);
        }

        writer.WriteEndObject();
    }
}
/// <summary>
/// Boolean converter
/// </summary>
public class BoolConverter : JsonConverter<bool>
{
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
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
         => reader.GetInt32() == 1;

    
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
    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value ? 1 : 0);
}