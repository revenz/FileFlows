namespace FileFlows.Plugin;

public enum FlowElementType
{
    Input,
    Output,
    Process,
    Logic,
    BuildStart,
    BuildEnd,
    BuildPart,
    Failure,
    Communication,
    Script,
    SubFlow
}

/// <summary>
/// Form Input Types
/// </summary>
public enum FormInputType
{
    /// <summary>
    /// Text
    /// </summary>
    Text = 1,
    /// <summary>
    /// Swtich
    /// </summary>
    Switch = 2,
    /// <summary>
    /// Select
    /// </summary>
    Select = 3,
    /// <summary>
    /// TextArea
    /// </summary>
    TextArea = 4,
    /// <summary>
    /// Code
    /// </summary>
    Code = 5,
    /// <summary>
    /// Int
    /// </summary>
    Int = 6,
    /// <summary>
    /// Float
    /// </summary>
    Float = 7,
    /// <summary>
    /// String Array
    /// </summary>
    StringArray = 8,
    /// <summary>
    /// File
    /// </summary>
    File = 9,
    /// <summary>
    /// Folder
    /// </summary>
    Folder = 10,
    /// <summary>
    /// Log View
    /// </summary>
    LogView = 11,
    /// <summary>
    /// Regular Expression
    /// </summary>
    RegularExpression = 12,
    /// <summary>
    /// Text Variable
    /// </summary>
    TextVariable = 13,
    /// <summary>
    /// Key Value
    /// </summary>
    KeyValue = 14,
    /// <summary>
    /// Label
    /// </summary>
    Label = 15,
    /// <summary>
    /// Horizontal Rule
    /// </summary>
    HorizontalRule = 16,
    /// <summary>
    /// Schedule
    /// </summary>
    Schedule = 17,
    /// <summary>
    /// Slider
    /// </summary>
    Slider = 18,
    /// <summary>
    /// Checklist
    /// </summary>
    Checklist = 19,
    /// <summary>
    /// Text Label
    /// </summary>
    TextLabel = 20,
    /// <summary>
    /// Password
    /// </summary>
    Password = 21,
    /// <summary>
    /// Executed Nodes
    /// </summary>
    ExecutedNodes = 22,
    /// <summary>
    /// Table
    /// </summary>
    Table = 23,
    /// <summary>
    /// Widget
    /// </summary>
    Widget = 24,
    /// <summary>
    /// Metadata
    /// </summary>
    Metadata = 25,
    /// <summary>
    /// Period
    /// </summary>
    Period = 26,
    /// <summary>
    /// File Size
    /// </summary>
    FileSize = 27,
    /// <summary>
    /// Button
    /// </summary>
    Button = 28,
    /// <summary>
    /// Color
    /// </summary>
    Color = 29,
    /// <summary>
    /// Template
    /// </summary>
    Template = 30,
    /// <summary>
    /// Number Percent
    /// </summary>
    NumberPercent = 31,
    /// <summary>
    /// Time
    /// </summary>
    Time = 32,
    /// <summary>
    /// Date
    /// </summary>
    Date = 33,
    /// <summary>
    /// Icon Picker
    /// </summary>
    IconPicker = 34,
    /// <summary>
    /// Math Value
    /// </summary>
    MathValue = 35,
    /// <summary>
    /// Date Range
    /// </summary>
    DateRange = 36,
    /// <summary>
    /// Multi-select
    /// </summary>
    MultiSelect = 37,
    /// <summary>
    /// Raw HTML renderer
    /// </summary>
    Html = 38,
    /// <summary>
    /// Hidden element
    /// </summary>
    Hidden = 39,
    /// <summary>
    /// Date compare
    /// </summary>
    DateCompare = 40,
    /// <summary>
    /// Key Value Int
    /// </summary>
    KeyValueInt = 41,
    /// <summary>
    /// Comobobox
    /// </summary>
    Combobox = 42,
    /// <summary>
    /// A drop down is a modern select, with support for icons
    /// </summary>
    DropDown = 43,
    /// <summary>
    /// Input for Binary
    /// </summary>
    Binary = 44,
    /// <summary>
    /// Executed Flow Elements Renderer
    /// </summary>
    ExecutedFlowElementsRenderer = 45,
    /// <summary>
    /// Input for selecting tag(s)
    /// </summary>
    TagSelection = 46,
    /// <summary>
    /// Custom fields
    /// </summary>
    CustomFields = 47,
    /// <summary>
    /// Language selector
    /// </summary>
    LanguageSelector = 48,
    /// <summary>
    /// Languages selector
    /// </summary>
    LanguagesSelector = 49
}


/// <summary>
/// A type of script
/// </summary>
public enum ScriptType
{
    /// <summary>
    /// A script used in a flow
    /// </summary>
    Flow = 0,
    /// <summary>
    /// A script used by the system to process something
    /// </summary>
    System = 1,
    /// <summary>
    /// A shared script which can be imported into other scripts
    /// </summary>
    Shared = 2,
    /// <summary>
    /// Template scripts used in the Function editor
    /// </summary>
    Template = 3,
    /// <summary>
    /// A scripts used by webhooks
    /// </summary>
    Webhook = 4
}

/// <summary>
/// Specifies the scripting languages supported by the application.
/// </summary>
public enum ScriptLanguage
{
    /// <summary>
    /// JavaScript scripting language.
    /// </summary>
    JavaScript = 0,

    /// <summary>
    /// Shell scripting language (Bash or SH).
    /// </summary>
    Shell = 1,

    /// <summary>
    /// Batch scripting language for .bat files.
    /// </summary>
    Batch = 2,

    /// <summary>
    /// PowerShell scripting language.
    /// </summary>
    PowerShell = 3,
    
    /// <summary>
    /// CSharp Language
    /// </summary>
    CSharp = 4
}