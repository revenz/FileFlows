using FileFlows.Plugin.Types;

namespace FileFlows.ServerShared.Helpers;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FileFlows.Plugin.Attributes;
using FileFlows.Shared.Models;
using FileFlows.Plugin;

/// <summary>
/// Helper used for UI Forms
/// </summary>
public class FormHelper
{
    /// <summary>
    /// Gets the form fields of a give type of object
    /// </summary>
    /// <param name="type">The type of object to load the form fields of</param>
    /// <param name="model">The model used to bind to the form fields</param>
    /// <returns>A list of all form fields</returns>
    public static List<ElementField> GetFields(Type type, IDictionary<string, object> model)
    {
        var fields = new List<ElementField>();
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            FormInputAttribute? attribute = null;
            try
            {
                attribute = prop.GetCustomAttributes(typeof(FormInputAttribute), false).FirstOrDefault() as FormInputAttribute;
                if (attribute == null)
                    continue;
            }
            catch (Exception)
            {
                continue;
            }

            var ef = new ElementField
            {
                Name = prop.Name,
                Order = attribute.Order,
                InputType = attribute.InputType,
                Type = prop.PropertyType.FullName ?? string.Empty,
                Parameters = new Dictionary<string, object>(),
                Validators = new List<Validators.Validator>(),
                ChangeValues = new ()
            };

            fields.Add(ef);

            foreach (var attProp in attribute.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (new string[] { nameof(FormInputAttribute.Order), nameof(FormInputAttribute.InputType), "TypeId" }.Contains(attProp.Name))
                    continue;

                var value = attProp.GetValue(attribute);
                if(value != null)
                    Logger.Instance.DLog(attProp.Name, value);
                ef.Parameters.Add(attProp.Name, attProp.GetValue(attribute)!);

            }

            if(attribute is ChecklistAttribute chk)
            {
                // get the options
                if(string.IsNullOrWhiteSpace(chk.OptionsProperty) == false)
                {
                    var chkPropertyValue = GetStaticProperty(type, chk.OptionsProperty);
                    if(chkPropertyValue != null)
                    {
                        try
                        {
                            ef.Parameters ??= new Dictionary<string, object>();
                            if(ef.Parameters.ContainsKey("Options") == false && chkPropertyValue is List<ListOption> options)
                                ef.Parameters.Add("Options", options);
                        }                                
                        catch (Exception){}
                    }
                }
            }
            if (attribute is SelectAttribute sel)
            {
                // get the options
                if (string.IsNullOrWhiteSpace(sel.OptionsProperty) == false)
                {
                    var selPropertyValue = GetStaticProperty(type, sel.OptionsProperty);
                    if (selPropertyValue != null)
                    {
                        try
                        {
                            ef.Parameters ??= new Dictionary<string, object>();
                            if (ef.Parameters.ContainsKey("Options") == false && selPropertyValue is List<ListOption> options)
                                ef.Parameters.Add("Options", options);
                        }
                        catch (Exception) { }
                    }
                }
            }
            if (attribute is TemplateAttribute template)
            {
                // get the options
                if (string.IsNullOrWhiteSpace(template.OptionsProperty) == false)
                {
                    var selPropertyValue = GetStaticProperty(type, template.OptionsProperty);
                    if (selPropertyValue != null)
                    {
                        try
                        {
                            ef.Parameters ??= new Dictionary<string, object>();
                            if (ef.Parameters.ContainsKey("Options") == false && selPropertyValue is List<ListOption> options)
                                ef.Parameters.Add("Options", options);
                        }
                        catch (Exception) { }
                    }
                }
            }

            if(attribute is KeyValueAttribute keyvalue)
            {
                // get the options
                if(string.IsNullOrWhiteSpace(keyvalue.OptionsProperty) == false)
                {
                    var chkPropertyValue = GetStaticProperty(type, keyvalue.OptionsProperty);
                    if(chkPropertyValue != null)
                    {
                        try
                        {
                            ef.Parameters ??= new Dictionary<string, object>();
                            if(ef.Parameters.ContainsKey("Options") == false && chkPropertyValue is List<ListOption> options)
                                ef.Parameters.Add("Options", options);
                        }                                
                        catch (Exception){}
                    }
                }
            }

            if (attribute is NumberPercentAttribute numPercent)
            {
                model.Add(prop.Name, new NumberPercent()
                {
                    Value = numPercent.DefaultValue,
                    Percentage = numPercent.DefaultPercentage
                });
            }
            
            if (model.ContainsKey(prop.Name) == false)
            {
                var dValue = prop.GetCustomAttributes(typeof(DefaultValueAttribute), false).FirstOrDefault() as DefaultValueAttribute;
                model.Add(prop.Name, (dValue != null ? dValue.Value : prop.PropertyType.IsValueType ? Activator.CreateInstance(prop.PropertyType) : null)!);
            }


            if (prop.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault() != null)
                ef.Validators.Add(new Validators.Required());
            if (prop.GetCustomAttributes(typeof(RangeAttribute), false).FirstOrDefault() is RangeAttribute range)
                ef.Validators.Add(new Validators.Range { Minimum = (int)range.Minimum, Maximum = (int)range.Maximum });
            if (prop.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RegularExpressionAttribute), false).FirstOrDefault() is System.ComponentModel.DataAnnotations.RegularExpressionAttribute exp)
                ef.Validators.Add(new Validators.Pattern { Expression = exp.Pattern });

            var conditionEquals = prop.GetCustomAttributes(typeof(ConditionEqualsAttribute), false).FirstOrDefault() as ConditionEqualsAttribute;
            if (conditionEquals != null)
            {
                ef.Conditions ??= new List<Condition>();
                ef.Conditions.Add(new Condition()
                {
                    Property = conditionEquals.Property,
                    Value = conditionEquals.Value,
                    IsNot = conditionEquals.Inverse
                });
            }


            foreach (ChangeValueAttribute cva in prop.GetCustomAttributes(typeof(ChangeValueAttribute), false) ?? new ChangeValueAttribute[]{})
            {
                ef.ChangeValues ??= new ();
                ef.ChangeValues.Add(new ()
                {
                    Property = cva.Property,
                    Value = cva.Value,
                    WhenValue = cva.WhenValue,
                    WhenValueIsNot = cva.WhenValueIsNot
                });
            }

        }
        return fields;
    }

    private static object? GetStaticProperty(Type type, string name)
    {
        if (type == null || type.Name == "Node" || type == typeof(object))
            return null;
            
        var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
        if (prop == null)
            return GetStaticProperty(type.BaseType!, name);
        return prop.GetValue(null);
    }
}
