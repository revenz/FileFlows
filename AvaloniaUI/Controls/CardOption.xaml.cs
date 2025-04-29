using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using FileFlows.AvaloniaUi.Helpers;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace FileFlows.AvaloniaUi.Controls;

/// <summary>
/// Card Option
/// </summary>
public partial class CardOption : UserControl
{
    public CardOption()
    {
        InitializeComponent();
        this.Tapped += (_, _) =>
        {
            SelectedValue = Value;
            ValueSelected?.Invoke(this, Value);
        };
    }

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<CardOption, string>(nameof(Title));

    public static readonly StyledProperty<string> DescriptionProperty =
        AvaloniaProperty.Register<CardOption, string>(nameof(Description));

    public static readonly StyledProperty<string> IconPathProperty =
        AvaloniaProperty.Register<CardOption, string>(nameof(IconPath));

    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<CardOption, string>(nameof(Value));

    public static readonly StyledProperty<string?> SelectedValueProperty =
        AvaloniaProperty.Register<CardOption, string?>(nameof(SelectedValue));

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string IconPath
    {
        get => GetValue(IconPathProperty);
        set => SetValue(IconPathProperty, value);
    }

    public string Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? SelectedValue
    {
        get => GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }

    public event EventHandler<string>? ValueSelected;

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        this.AttachedToVisualTree += (_, _) => ApplyValues();
        this.PropertyChanged += (_, e) =>
        {
            if (e.Property == TitleProperty || e.Property == DescriptionProperty || e.Property == IconPathProperty || e.Property == SelectedValueProperty)
                ApplyValues();
        };
    }

    private void ApplyValues()
    {
        this.FindControl<TextBlock>("TitleText")!.Text = Title;
        this.FindControl<TextBlock>("DescriptionText")!.Text = Description;
        if (string.IsNullOrWhiteSpace(IconPath) == false)
        {
            var uri = new Uri(IconPath);
            using var stream = AssetLoader.Open(uri);
            this.FindControl<Image>("Icon")!.Source = new Bitmap(stream);
        }

        bool isSelected = Value == SelectedValue;

        Color accent = SystemAccentColor.GetOsAccentColor();
        Brush brush = new SolidColorBrush(accent);
        Brush brushO = new SolidColorBrush(accent, 0.025);
        
        this.FindControl<Border>("RootBorder")!.BorderBrush = isSelected ? brush : Brushes.Transparent;
        this.FindControl<Border>("RootBorder")!.Background = isSelected ? brushO : Brushes.Transparent;
    }
}