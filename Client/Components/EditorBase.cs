using System.Text.Json;
using System.Text.RegularExpressions;
using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;

namespace FileFlows.Client.Components;

/// <summary>
/// Interface for an editor
/// </summary>
public abstract class EditorBase : InputRegister
{
    /// <summary>
    /// Gets or sets the model to bind to the editor
    /// </summary>
    public virtual ExpandoObject Model { get; set; }

    /// <summary>
    /// Get the name of the type this editor is editing
    /// </summary>
    public virtual string TypeName { get; set; }

    /// <summary>
    /// Gets or sets the elemetn fields
    /// </summary>
    protected virtual List<IFlowField> Fields { get; set; }

    /// <summary>
    /// Finds a field by its name
    /// </summary>
    /// <param name="name">the name of the field</param>
    /// <returns>the field if found, otherwise null</returns>
    internal ElementField? FindField(string name)
    {
        var field = this.Fields?.FirstOrDefault(x => x is ElementField ef && ef.Name == name) as ElementField;
        return field;
    }

    /// <summary>
    /// Finds an input by name and its type
    /// </summary>
    /// <param name="name">the element field of the input</param>
    /// <typeparam name="T">the type of field</typeparam>
    /// <returns>the input if found</returns>
    internal T? FindInput<T>(string name)
    {
        var input = this.RegisteredInputs.Values.FirstOrDefault(x => x.Field?.Name == name && x is T);
        return input == null ? default : (T)input;
    }

    /// <summary>
    /// Updates a value
    /// </summary>
    /// <param name="field">the field whose value is being updated</param>
    /// <param name="value">the value of the field</param>
    internal void UpdateValue(ElementField field, object value)
    {
        if (field.UiOnly)
            return;
        if (Model == null)
            return;
        var dict = (IDictionary<string, object>)Model!;
        if (dict.ContainsKey(field.Name))
            dict[field.Name] = value;
        else
            dict.Add(field.Name, value);
    }

    /// <summary>
    /// Gets a parameter value for a field
    /// </summary>
    /// <param name="field">the field to get the value for</param>
    /// <param name="parameter">the name of the parameter</param>
    /// <param name="default">the default value if not found</param>
    /// <typeparam name="T">the type of parameter</typeparam>
    /// <returns>the parameter value</returns>
    internal T GetParameter<T>(ElementField field, string parameter, T @default = default(T))
    {
        var dict = field?.Parameters as IDictionary<string, object>;
        if (dict?.ContainsKey(parameter) != true)
            return @default;
        var val = dict[parameter];
        if (val == null)
            return @default;
        try
        {
            var converted = Converter.ConvertObject(typeof(T), val);
            T result = (T)converted;
            if(result is List<ListOption> options)
            {
                foreach(var option in options)
                {
                    if(option.Value is JsonElement je)
                    {
                        if (je.ValueKind == JsonValueKind.String)
                            option.Value = je.GetString();
                        else if (je.ValueKind == JsonValueKind.Number)
                            option.Value = je.GetInt32();
                    }
                }
            }

            return result;
        }
        catch (Exception)
        {
            Logger.Instance.ELog("Failed converted: " + parameter, val);
            return @default;
        }
    }
    
    /// <summary>
    /// Gets the minimum and maximum from a range validator (if exists)
    /// </summary>
    /// <param name="field">The field to get the range for</param>
    /// <returns>the range</returns>
    internal (int min, int max) GetRange(ElementField field)
    {
        var range = field?.Validators?.Where(x => x is Validators.Range)?.FirstOrDefault() as Validators.Range;
        return range == null ? (0, 0) : (range.Minimum, range.Maximum);
    }

    /// <summary>
    /// Gets the default of a specific type
    /// </summary>
    /// <param name="type">the type</param>
    /// <returns>the default value</returns>
    private object GetDefault(Type type)
    {
        if(type?.IsValueType == true)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }

    /// <summary>
    /// Gets a value for a field
    /// </summary>
    /// <param name="field">the field whose value to get</param>
    /// <param name="type">the type of value to get</param>
    /// <returns>the value</returns>
    internal object GetValue(string field, Type type)
    {
        if (Model == null)
        {
            Logger.Instance.ILog("GetValue: Model was null");
            return GetDefault(type);
        }

        var dict = (IDictionary<string, object>)Model!;
        if (dict.ContainsKey(field) == false)
        {
            Logger.Instance.ILog("GetValue: Model does not contain key: " + field);
            return GetDefault(type);
        }

        object value = dict[field];
        if (value == null)
        {
            Logger.Instance.ILog("GetValue: value was null");
            return GetDefault(type);
        }

        if (value is JsonElement je)
        {
            Logger.Instance.ILog("GetValue: value is json element");
            if (type == typeof(string))
                return je.GetString();
            if (type== typeof(int))
                return je.GetInt32();
            if (type == typeof(bool))
                return je.GetBoolean();
            if (type == typeof(float))
                return (float)je.GetInt64();
        }

        if (value.GetType().IsAssignableTo(type))
        {
            Logger.Instance.ILog("GetValue: value is assignable to type");
            return value;
        }

        try
        {
            Logger.Instance.ILog($"GetValue: trying to convert to type '{value.GetType()}' to '{type}'");
            return Converter.ConvertObject(type, value);
        }
        catch(Exception ex)
        {
            Logger.Instance.ILog("GetValue: failed converting ot type, returning default: " + ex.Message);
            return GetDefault(type);
        }
    }
    
    /// <summary>
    /// Gets a value for a field
    /// </summary>
    /// <param name="field">the field whose value to get</param>
    /// <param name="default">the default value if none is found</param>
    /// <typeparam name="T">the type of value to get</typeparam>
    /// <returns>the value</returns>
    internal T GetValue<T>(string field, T @default = default)
    {
        if (Model == null)
            return @default;
        var dict = (IDictionary<string, object>)Model!;
        if (dict.ContainsKey(field) == false)
        {
            return @default;
        }
        object value = dict[field];
        if (value == null)
        {
            return @default;
        }

        if (value is JsonElement je)
        {
            if (typeof(T) == typeof(string))
            {
                if (je.ValueKind == JsonValueKind.Number)
                    return (T)(object)je.ToString();
                return (T)(object)je.GetString()!;
            }

            if (typeof(T) == typeof(int))
                return (T)(object)je.GetInt32();
            if (typeof(T) == typeof(bool))
            {
                if (je.ValueKind == JsonValueKind.False)
                    return (T)(object)false;
                if (je.ValueKind == JsonValueKind.True)
                    return (T)(object)true;
                if (je.ValueKind == JsonValueKind.String)
                {
                    var str = je.GetString().ToLowerInvariant();
                    return (T)(object)(str == "true" || str == "1");
                }
                if (je.ValueKind == JsonValueKind.Number)
                    return (T)(object)(je.GetInt32() > 0);

                return (T)(object)false;
            }

            if (typeof(T) == typeof(float))
            {
                try
                {
                    return (T)(object)(float)je.GetInt64();
                }
                catch (Exception)
                {
                    return (T)(object)(float.Parse(je.ToString()));
                }
            }
        }

        if (value is T)
        {
            return (T)value;
        }

        try
        {
            return (T)Converter.ConvertObject(typeof(T), value);
        }
        catch(Exception)
        {
            return default;
        }
    }

    /// <summary>
    /// Converts an object to an ExpandoObject
    /// </summary>
    /// <param name="model">the model to convert</param>
    /// <returns>the expanod object</returns>
    protected ExpandoObject ConvertToExando(object model)
    {
        if (model == null)
            return new ExpandoObject();
        if (model is ExpandoObject eo)
            return eo;

        var expando = new ExpandoObject();
        var dictionary = (IDictionary<string, object>)expando!;

        foreach (var property in model.GetType().GetProperties())
        {
            if (property.CanRead == false)
                continue;
            dictionary.Add(property.Name, property.GetValue(model));
        }

        return expando;
    }
    
    /// <summary>
    /// Converts the model to json for comparing
    /// </summary>
    /// <param name="model">the model</param>
    /// <returns>the model json</returns>
    protected string ModelToJsonForCompare(ExpandoObject model)
    {
        string json = model == null ? string.Empty : JsonSerializer.Serialize(Model);
        json = json.Replace("[]", "null");
        // remove default values, templates do not always set these
        json = Regex.Replace(json, @"\""[^\""]+""[\s]*:[\s]*(null|false|0)(,)?", string.Empty);
        while (json.IndexOf(",,", StringComparison.Ordinal) > 0)
            json = json.Replace(",,", ",");
        json = json.Replace(",}", "}");
        return json;
    }
}



/// <summary>
/// UI Button
/// </summary>
public class ActionButton
{
    private string _Label;

    /// <summary>
    /// Gets or sets the label of the button
    /// </summary>
    public string Label
    {
        get => _Label;
        set => _Label = Translater.TranslateIfNeeded(value);
    }
    
    /// <summary>
    /// Gets or sets an optional UID for the button
    /// </summary>
    public string? Uid { get; set; }

    /// <summary>
    /// Gets or sets the click action
    /// </summary>
    public Action<object, EventArgs> Clicked { get; set; }
}

/// <summary>
/// Arguments used when opening the editor
/// </summary>
public class EditorOpenArgs
{
    /// <summary>
    /// Gets or sets the type name used in translation
    /// </summary>
    public string TypeName { get; set; }
    /// <summary>
    /// Gets or sets the title of the editor
    /// </summary>
    public string Title { get; set; }
    /// <summary>
    /// Gets or sets the description to show, if not set, editor will try to find a description using the TypeName
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// Gets or sets the main fields to show in the editor
    /// </summary>
    public List<IFlowField> Fields { get; set; }
    /// <summary>
    /// Gets or sets a callback that is called when the editor is saved
    /// </summary>
    public Editor.SaveDelegate SaveCallback  { get; set; }
    /// <summary>
    /// Gets or sets if the editor is readonly
    /// </summary>
    public bool ReadOnly  { get; set; }
    /// <summary>
    /// Gets or sets if the editor is a large editor and takes up more width
    /// </summary>
    public bool Large  { get; set; }

    /// <summary>
    /// Gets or sets if inputs should be full width and not use a maximum width
    /// </summary>
    public bool FullWidth { get; set; }

    /// <summary>
    /// Gets or sets the label to show on the save button
    /// </summary>
    public string SaveLabel  { get; set; }
    /// <summary>
    /// Gets or sets the label to show on the cancel button
    /// </summary>
    public string CancelLabel  { get; set; }
    /// <summary>
    /// Gets or sets any additional fields ot show
    /// </summary>
    public RenderFragment AdditionalFields  { get; set; }
    /// <summary>
    /// Gets or sets the tabs for the editor
    /// </summary>
    public Dictionary<string, List<IFlowField>> Tabs { get; set; }
    /// <summary>
    /// Gets or sets the URL for the help button
    /// </summary>
    public string HelpUrl  { get; set; }
    /// <summary>
    /// Gets or sets it the title should not be translated
    /// </summary>
    public bool NoTranslateTitle  { get; set; }
    /// <summary>
    /// Gets or sets the label to shown on the download button
    /// </summary>
    public string DownloadButtonLabel { get; set; } = "Labels.Download";
    /// <summary>
    /// Gets or sets the URL for the download button
    /// </summary>
    public string DownloadUrl { get; set; }
    /// <summary>
    /// Gets or sets if a prompt should be shown the user if they try to close the editor with changes
    /// </summary>
    public bool PromptUnsavedChanges  { get; set; }
    /// <summary>
    /// Gets or sets 
    /// </summary>
    public IEnumerable<ActionButton> AdditionalButtons { get; set; }
    
    /// <summary>
    /// Gets or sets if the fields scrollbar should be hidden
    /// </summary>
    public bool HideFieldsScroller { get; set; }
    /// <summary>
    /// Gets or sets the model to bind to the editor
    /// </summary>
    public object Model { get; set; }
}