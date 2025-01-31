using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections;
using System.Reflection;

namespace FileFlows.WebServer.Filters;

/// <summary>
/// An action filter that trims all string properties in the action arguments.
/// </summary>
public class TrimStringsFilter : IActionFilter
{
    /// <summary>
    /// Called before the action method is executed.
    /// This method inspects the action arguments and trims all string properties.
    /// </summary>
    /// <param name="context">The context for the action.</param>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Iterate through each action argument
        foreach (var key in context.ActionArguments.Keys.ToList())
        {
            var argument = context.ActionArguments[key];
            if (argument != null)
            {
                // Trim strings within the argument recursively
                context.ActionArguments[key] = TrimStrings(argument);
            }
        }
    }

    /// <summary>
    /// Called after the action method is executed.
    /// This method does not perform any actions in this filter.
    /// </summary>
    /// <param name="context">The context for the action.</param>
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No implementation needed for this example.
    }

    /// <summary>
    /// Recursively trims all string properties within an object.
    /// </summary>
    /// <param name="obj">The object to trim.</param>
    /// <returns>The trimmed object.</returns>
    private object TrimStrings(object obj)
    {
        if (obj == null)
            return null;

        if (obj is string str)
        {
            // If the object is a string, return the trimmed string
            return str.Trim();
        }
        
        if (obj is IList list)
        {
            // If the object is a list, iterate through each item and trim it
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = TrimStrings(list[i]);
            }
        }
        else if (obj is IEnumerable enumerable and not string)
        {
            // If the object is a collection (excluding strings and lists), iterate through each item and trim it
            var items = enumerable.Cast<object>().ToList();
            for (int i = 0; i < items.Count; i++)
            {
                items[i] = TrimStrings(items[i]);
            }

            if (obj.GetType().IsArray)
            {
                // If it's an array, create a new array of the same type and copy the trimmed items
                var elementType = obj.GetType().GetElementType();
                var trimmedArray = Array.CreateInstance(elementType, items.Count);
                Array.Copy(items.ToArray(), trimmedArray, items.Count); // Use Array.Copy to copy items into trimmedArray
                return trimmedArray;
            }
            else
            {
                // If it's another type of enumerable, try to set the trimmed items back
                var ctor = obj.GetType().GetConstructor([typeof(IEnumerable<>)]);
                if (ctor != null)
                {
                    return ctor.Invoke([items]);
                }
            }
        }
        else
        {
            // Use reflection to iterate over properties of the object
            var properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in properties)
            {
                if (property is not { CanRead: true, CanWrite: true }) 
                    continue;
                
                var propertyType = property.PropertyType;
                if (propertyType.IsPrimitive || propertyType == typeof(decimal) || propertyType == typeof(DateTime) || propertyType == typeof(Guid))
                {
                    continue; // Skip primitive types and other specified types
                }
                
                var value = property.GetValue(obj);
                if (value != null)
                {
                    // Recursively trim nested properties and set the trimmed object back
                    property.SetValue(obj, TrimStrings(value));
                }
            }
        }

        // Return the trimmed object
        return obj;
    }
}