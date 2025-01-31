namespace FileFlows.Shared;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// A class used to convert objects from one type to another
/// </summary>
public class Converter
{
    /// <summary>
    /// Converts an object to a specific type
    /// </summary>
    /// <param name="type">The type to convert to</param>
    /// <param name="value">The object to convert</param>
    /// <param name="logger">Optional logger to use for logging</param>
    /// <returns>The converted object</returns>
    public static object ConvertObject(Type type, object? value, ILogger? logger = null)
    {
        if (value == null)
            return Activator.CreateInstance(type)!;
        Type valueType = value.GetType();
        if (value is JsonElement je)
        {
            string json = je.GetRawText();
            if (type.IsEnum && int.TryParse(json, out int result))
                return result;
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize(json, type, options)!;
            }
            catch (Exception)
            {
                logger?.ELog("Failed deserializing JsonElement Value: " + json);
            }
        }
        if (valueType == type)
            return value;
        
        if (type == typeof(Type))
            return value;

        if (type.IsArray && typeof(IEnumerable).IsAssignableFrom(valueType))
            return ChangeListToArray(type.GetElementType()!, (IEnumerable)value, valueType);


        // not used yet, so not tested
        // if (valueType.IsArray && typeof(IEnumerable).IsAssignableFrom(type))
        //     return ChangeArrayToList(type.GetElementType(), (Array)value);

        if (valueType == typeof(Int64) && type == typeof(Int32))
            return Convert.ToInt32(value);
        if(type == typeof(List<object>) && value is IEnumerable enumerable)
        {
            List<object> result = new();
            foreach (object item in enumerable)
                result.Add(item);
            return result;
        }
        return Convert.ChangeType(value, type);
    }

    /// <summary>
    /// Converts a IEnumeable to an array
    /// </summary>
    /// <param name="value">The IEnumerable to convert</param>
    /// <param name="valueType">The type of array to create</param>
    /// <typeparam name="T">the type of the array to create</typeparam>
    /// <returns>An array of the IEnumerable</returns>
    public static object ChangeListToArray<T>(IEnumerable value, Type valueType)
    {
        var arrayType = typeof(T).GetElementType();
        return ChangeListToArray(arrayType!, value, valueType);
    }
    
    /// <summary>
    /// Converts a IEnumerable to an list
    /// </summary>
    /// <param name="arrayType">the type of the array to create</param>
    /// <param name="value">The IEnumerable to convert</param>
    /// <param name="valueType">The type of array to create</param>
    /// <returns>An list of the IEnumerable</returns>
    public static object ChangeListToArray(Type arrayType, IEnumerable value, Type valueType)
    {
        Logger.Instance.DLog("Change list to array");
        List<object> list = new List<object>();
        foreach (var o in value)
            list.Add(o);
        var array = Array.CreateInstance(arrayType, list.Count);
        for (int i = 0; i < list.Count; i++)
            array.SetValue(list[i], i);
        return array;
    }
}