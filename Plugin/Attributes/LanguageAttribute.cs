namespace FileFlows.Plugin.Attributes;

/// <summary>
/// Language attribute
/// </summary>
/// <param name="order">the order this field appears</param>
public class LanguageAttribute(int order) : FormInputAttribute(FormInputType.LanguageSelector, order)
{
}

/// <summary>
/// Languages attribute
/// </summary>
/// <param name="order">the order this field appears</param>
public class LanguagesAttribute(int order) : FormInputAttribute(FormInputType.LanguagesSelector, order)
{
}