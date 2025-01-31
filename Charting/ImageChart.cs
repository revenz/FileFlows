using System.Text;
using SixLabors.ImageSharp.Formats.Png;

namespace FileFlows.Charting;

/// <summary>
/// Image chart base that all image charts inherit from
/// </summary>
public abstract class ImageChart
{
    /// <summary>
    /// The width for emailed charts
    /// </summary>
    public const int EmailChartWidth = 560;
    /// <summary>
    /// The height for emailed charts
    /// </summary>
    public const int EmailChartHeight = 200;
    
    /// <summary>
    /// The brush used for text
    /// </summary>
    protected readonly static SolidBrush TextBrush = null!;
    /// <summary>
    /// The pen used for text
    /// </summary>
    protected readonly static Pen TextPen = null!;
    /// <summary>
    /// The color used for lines on the chart
    /// </summary>
    protected static Rgba32 LineColor;
    
    /// <summary>
    /// The colors to show on the chart
    /// </summary>
    public static readonly string[] COLORS =
    [
        // same colors as JS
        "#33b2df","#33DF55A6", "#84004bd9", "#007bff", "#6610f2",
        "#17a2b8", "#fd7e14", "#28a745", "#20c997", "#ffc107",  "#ff4d76",
        // different colors
        "#007bff", "#28a745", "#ffc107", "#dc3545", "#17a2b8",
        "#fd7e14", "#6f42c1", "#20c997", "#6610f2", "#e83e8c",
        "#ff6347", "#4682b4", "#9acd32", "#8a2be2", "#ff4500",
        "#2e8b57", "#d2691e", "#ff1493", "#00ced1", "#b22222",
        "#daa520", "#5f9ea0", "#7f007f", "#808000", "#3cb371"
    ];
    /// <summary>
    /// The font to use in the charts
    /// </summary>
    protected static Font Font = null!;

    /// <summary>
    /// Get the scaling of the image, so we draw larger for better quality
    /// </summary>
    protected const float Scale = 2f;

    /// <summary>
    /// The base application directory to load the resources from
    /// </summary>
    protected static string BaseDirectory = null!;
   
    /// <summary>
    /// Static constructor for the image chart
    /// </summary>
    /// <exception cref="Exception">throws an exception if an error occurs</exception>
    static ImageChart ()
    {
        try
        {
            if (string.IsNullOrEmpty(BaseDirectory))
            {
                var dllDir =
                    System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(dllDir))
                    throw new Exception("Failed to find DLL directory");
                BaseDirectory = new DirectoryInfo(dllDir).Parent?.FullName ?? string.Empty;
            }

            TextBrush = new(Color.White);
            TextPen = Pens.Solid(Color.White, 1);
            LineColor = Rgba32.ParseHex("#afafaf");

            if (Font != null)
                return;
#if (DEBUG)
            var dir = "wwwroot";
#else
        var dir = System.IO.Path.Combine(BaseDirectory, "Server/wwwroot");
#endif
            string font = System.IO.Path.Combine(dir, "report-font.ttf");
            FontCollection collection = new();
            var family = collection.Add(font);
            // collection.TryGet("Font Name", out FontFamily font);
            Font = family.CreateFont(11 * Scale, FontStyle.Regular);
        }
        catch (Exception ex) when (ChartHelper.Logger != null)
        {
            ChartHelper.Logger.ELog(
                "Error initializing ImageChart: " + ex.Message + Environment.NewLine + ex.StackTrace);
        }
    }

    /// <summary>
    /// Gets the image size to use
    /// </summary>
    /// <param name="customWidth">the custom width</param>
    /// <param name="customHeight">the custom height</param>
    /// <returns>the image size</returns>
    protected (int Width, int Height) GetImageSize(int? customWidth, int? customHeight)
    {
        float width = customWidth ?? EmailChartWidth;
        float height = customHeight ?? EmailChartHeight;
        width *= Scale;
        height *= Scale;
        return ((int)Math.Round(width), (int)Math.Round(height));
    }


    /// <summary>
    /// Converts an Image object to a base64-encoded PNG and returns an HTML img tag.
    /// </summary>
    /// <param name="image">The Image object to convert.</param>
    /// <returns>An HTML img tag with base64-encoded PNG.</returns>
    public string ImageToBase64ImgTag(Image<Rgba32> image)
    {
        using MemoryStream memoryStream = new MemoryStream();
        // Save the Image to the memory stream as PNG
        image.Save(memoryStream, new PngEncoder());

        // Convert the image to base64
        string base64Image = Convert.ToBase64String(memoryStream.ToArray());

        // Construct the img tag
        StringBuilder imgTagBuilder = new StringBuilder();
        imgTagBuilder.Append("<img ");
        imgTagBuilder.Append($"src=\"data:image/png;base64,{base64Image}\" ");
        imgTagBuilder.Append($"style=\"width:100%;height:auto\" ");
        imgTagBuilder.Append("/>");

        return imgTagBuilder.ToString();
    }
}