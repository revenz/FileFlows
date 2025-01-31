using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FileFlows.Plugin.Attributes;
using FileFlows.Shared.Models;

namespace FileFlows.Server.Helpers;

/// <summary>
/// Helper for the UI Form
/// </summary>
class FormHelper
{
    /// <summary>
    /// Gets the fields for a given type
    /// </summary>
    /// <param name="type">the Type to get the fields for</param>
    /// <param name="model">the model to bind the fields to</param>
    /// <returns>the fields</returns>
    public static List<ElementField> GetFields(Type type, IDictionary<string, object> model)
    {
        var fields = new List<ElementField>();
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (prop.GetCustomAttributes(typeof(FormInputAttribute), false).FirstOrDefault() is FormInputAttribute
                    attribute == false)
                continue;
        
            var ef = new ElementField
            {
                Name = prop.Name,
                Order = attribute.Order,
                InputType = attribute.InputType,
                Type = prop.PropertyType.FullName,
                Parameters = new Dictionary<string, object>(),
                Validators = new List<Validators.Validator>()
            };

            fields.Add(ef);

            var parameters = new Dictionary<string, object>();

            foreach (var attProp in attribute.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (new string[] { nameof(FormInputAttribute.Order), nameof(FormInputAttribute.InputType), "TypeId" }.Contains(attProp.Name))
                    continue;

                var value = attProp?.GetValue(attribute);
                ef.Parameters.Add(attProp.Name, attProp.GetValue(attribute));

            }

            if (model.ContainsKey(prop.Name) == false)
            {
                var dValue = prop.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute;
                model.Add(prop.Name, dValue != null ? dValue.Value : prop.PropertyType.IsValueType ? Activator.CreateInstance(prop.PropertyType) : null);
            }


            if (prop.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault() != null)
                ef.Validators.Add(new Validators.Required());
            if (prop.GetCustomAttributes(typeof(RangeAttribute), false).FirstOrDefault() is RangeAttribute range)
                ef.Validators.Add(new Validators.Range { Minimum = (int)range.Minimum, Maximum = (int)range.Maximum });
            if (prop.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RegularExpressionAttribute), false).FirstOrDefault() is System.ComponentModel.DataAnnotations.RegularExpressionAttribute exp)
                ef.Validators.Add(new Validators.Pattern { Expression = exp.Pattern });
            
        }
        return fields;
    }
}