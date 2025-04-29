using FileFlows.Plugin;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileFlows.Client.Components.Widgets;

/// <summary>
/// Trend Widget
/// </summary>
public partial class TrendWidget : ComponentBase, IAsyncDisposable
{
    /// <summary>
    /// The UID of hte widget
    /// </summary>
    private readonly string Uid = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Gets or sets the JavaScript runtime
    /// </summary>
    [Inject] protected IJSRuntime jsRuntime { get; set; }
    
    /// <summary>
    /// Gets or sets the title of the widget
    /// </summary>
    [Parameter] public string Title { get; set; }
    
    /// <summary>
    /// Gets or sets the child content
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }
    
    /// <summary>
    /// Gets or sets the color of the widget
    /// </summary>
    [Parameter] public string Color { get; set; }
    /// <summary>
    /// Gets or sets the icon of the widget
    /// </summary>
    [Parameter] public string Icon { get; set; }
    /// <summary>
    /// Gets or sets the total of the widget
    /// </summary>
    [Parameter] public string Total { get; set; }
    /// <summary>
    /// Gets or sets the suffix of the widget
    /// </summary>
    [Parameter] public string Suffix { get; set; }
    
    /// <summary>
    /// Gets or sets the points
    /// </summary>
    [Parameter] public double[] Data { get; set; }

    /// <summary>
    /// Gets or sets the selected mode
    /// </summary>
    [Parameter]
    public int Mode { get; set; }

    /// <summary>
    /// Gets or sets the callback when the Mode changes
    /// </summary>
    [Parameter]
    public EventCallback<int> ModeChanged { get; set; }
    // Define the width and height of the SVG
    //const int svgWidth = 300;
    //const int svgHeight = 60;
    private int svgWidth, svgHeight;
    
    
    /// <summary>
    /// Reference to JS Report class
    /// </summary>
    private IJSObjectReference jsObjectReference;
    
    /// <summary>
    /// This method allows for propagating the changes properly
    /// </summary>
    /// <param name="newValue">the new value</param>
    private async Task OnModeChanged(int newValue)
    {
        Mode = newValue;
        await ModeChanged.InvokeAsync(newValue);
    }
    
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var jso = await jsRuntime.InvokeAsync<IJSObjectReference>("import",
                $"./Components/Widgets/TrendWidget/TrendWidget.razor.js?v={Globals.Version}");
            
            jsObjectReference = await jso.InvokeAsync<IJSObjectReference>("createTrendWidget", DotNetObjectReference.Create(this), Uid);
        }
    }

    /// <summary>
    /// Generates a SVG Image
    /// </summary>
    /// <param name="values">the values in the trend</param>
    /// <param name="color">the color</param>
    /// <returns>the Path svg</returns>
    public string? GenerateSvg(double[] values, string color)
    {
        if (values == null || values.Length < 4 || svgWidth < 1 || svgHeight < 1)
            return null;

        // Define the stroke color based on the input 'color'
        var stroke = color switch
        {
            "green" => "#4CAF50",
            "yellow" => "#FFC107",
            "purple" => "#FC54B3", // #9C27B0",
            _ => "#444CF7" // Default to blue
        };

        // Add transparency to the stroke color for the fill
        var fill = stroke + "1A"; // Adding '1A' for 10% transparency

        // Normalize values to fit within the height of the SVG
        double maxValue = values.Max();
        double minValue = values.Min();
        double range = maxValue - minValue;

        // Set a minimum range to avoid too small differences
        if (range == 0)
        {
            range = 1; // Avoid divide by zero
        }

        // Set the height scale factor to fit values in SVG
        double heightScale = svgHeight / range;

        // Calculate the X positions based on the index of values
        double xStep = svgWidth / (double)(values.Length - 1);

        // Generate the points for the curve
        var points = new List<string>();
        for (int i = 0; i < values.Length; i++)
        {
            double x = i * xStep;
            double y = svgHeight - ((values[i] - minValue) * heightScale); // Invert Y-axis for SVG
            points.Add($"{x},{y}");
        }

        // Create a list to store the path segments with Bezier curves
        var pathSegments = new List<string>();
        pathSegments.Add($"M {points[0]}"); // Move to the first point

        for (int i = 0; i < points.Count - 1; i++)
        {
            // Get the current and next point
            var currentPoint = points[i].Split(',').Select(double.Parse).ToArray();
            var nextPoint = points[i + 1].Split(',').Select(double.Parse).ToArray();

            // Calculate control points for smoothing
            double cpX1 = currentPoint[0] + (xStep / 2);
            double cpY1 = currentPoint[1];
            double cpX2 = nextPoint[0] - (xStep / 2);
            double cpY2 = nextPoint[1];

            // Add the Bezier curve segment
            pathSegments.Add($"C {cpX1},{cpY1} {cpX2},{cpY2} {nextPoint[0]},{nextPoint[1]}");
        }

        // Create the path for the filled area under the line
        string fillPathData =
            string.Join(" ", pathSegments) + $" L {svgWidth},{svgHeight} L 0,{svgHeight} Z"; // Close the fill path
        string pathData = string.Join(" ", pathSegments); // Path data for the stroke

        // Return the SVG markup
        return $@"<path fill='{fill}' d='{fillPathData}' />
        <path fill='none' stroke='{stroke}' stroke-width='2px' d='{pathData}' />";
    }

    /// <summary>
    /// Called when the component is resized
    /// </summary>
    /// <param name="width">the width</param>
    /// <param name="height">the height</param>
    [JSInvokable]
    public void OnResize(int width, int height)
    {
        svgHeight = height;
        svgWidth = width;
        
        StateHasChanged();
    }


    /// <summary>
    /// Disposes of the coomponent
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (jsObjectReference == null)
            return;
        await jsObjectReference.InvokeVoidAsync("dispose");
        await jsObjectReference.DisposeAsync();
    }
}