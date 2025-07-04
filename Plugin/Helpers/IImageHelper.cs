namespace FileFlows.Plugin.Helpers;

/// <summary>
/// Image helper
/// </summary>
public interface IImageHelper
{
    /// <summary>
    /// Gets the image info for a image file
    /// </summary>
    /// <param name="imagePath">The path to the image.</param>
    /// <returns>the image information</returns>
    Result<ImageInfo> GetInfo(string imagePath);
    
    /// <summary>
    /// Draws a red rectangle on the specified image at the specified coordinates and dimensions.
    /// </summary>
    /// <param name="imagePath">The path to the input image.</param>
    /// <param name="x">The x-coordinate of the top-left corner of the rectangle.</param>
    /// <param name="y">The y-coordinate of the top-left corner of the rectangle.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    void DrawRectangleOnImage(string imagePath, int x, int y, int width, int height);

    /// <summary>
    /// Gets the dimensions of an image
    /// </summary>
    /// <param name="imagePath">The path to the input image.</param>
    /// <returns>the dimensions</returns>
    Result<(int Width, int Height)> GetDimensions(string imagePath);

    /// <summary>
    /// Converts an image to JPG
    /// </summary>
    /// <param name="imagePath">The path to the input image.</param>
    /// <param name="destination">The destination where to save the new image to</param>
    /// <param name="options">The image options</param>
    /// <returns>true if successful, otherwise false</returns>
    Result<bool> ConvertToJpeg(string imagePath, string destination, ImageOptions? options = null);

    /// <summary>
    /// Converts an image to webp
    /// </summary>
    /// <param name="imagePath">The path to the input image.</param>
    /// <param name="destination">The destination where to save the new image to</param>
    /// <param name="options">The image options</param>
    /// <returns>true if successful, otherwise false</returns>
    Result<bool> ConvertToWebp(string imagePath, string destination, ImageOptions? options = null);

    /// <summary>
    /// Converts an image from one type to another
    /// </summary>
    /// <param name="imagePath">the path of the file to convert</param>
    /// <param name="destination">The destination where to save the new image to</param>
    /// <param name="type">the type of the new image</param>
    /// <param name="quality">the image quality, only used by some image types</param>
    /// <returns>true if successful, otherwise false</returns>
    Result<bool> ConvertImage(string imagePath, string destination, ImageType type, int quality);
    
    /// <summary>
    /// Resizes the image to the specified dimensions while maintaining the aspect ratio.
    /// </summary>
    /// <param name="imagePath">The path to the input image.</param>
    /// <param name="destination">The file path where the resized image will be saved.</param>
    /// <param name="width">The new width of the image. Pass in 0 to maintain the aspect ratio.</param>
    /// <param name="height">The new height of the image. Pass in 0 to maintain the aspect ratio.</param>
    /// <param name="mode">The resize mode to use.</param>
    /// <param name="type">the type of the new image</param>
    /// <param name="quality">the image quality, only used by some image types</param>
    /// <returns>A result indicating whether the operation was successful or not.</returns>
    Result<bool> Resize(string imagePath, string destination, int width, int height, ResizeMode mode, ImageType? type = null, int quality = 100);

    /// <summary>
    /// Flips an image vertically
    /// </summary>
    /// <param name="imagePath">The path to the input image.</param>
    /// <param name="destination">The file path where the resized image will be saved.</param>
    /// <param name="type">the type of the new image</param>
    /// <param name="quality">the image quality, only used by some image types</param>
    /// <returns>A result indicating whether the operation was successful or not.</returns>
    Result<bool> FlipVertically(string imagePath, string destination, ImageType? type = null, int quality = 100);
    
    /// <summary>
    /// Flips an image horizontally
    /// </summary>
    /// <param name="imagePath">The path to the input image.</param>
    /// <param name="destination">The file path where the resized image will be saved.</param>
    /// <param name="type">the type of the new image</param>
    /// <param name="quality">the image quality, only used by some image types</param>
    /// <returns>A result indicating whether the operation was successful or not.</returns>
    Result<bool> FlipHorizontally(string imagePath, string destination, ImageType? type = null, int quality = 100);
    
    /// <summary>
    /// Rotates an image by the degress given
    /// </summary>
    /// <param name="imagePath">The path to the input image.</param>
    /// <param name="degrees">the number of degrees to rotate the image by</param>
    /// <param name="destination">The file path where the resized image will be saved.</param>
    /// <param name="type">the type of the new image</param>
    /// <param name="quality">the image quality, only used by some image types</param>
    /// <returns>A result indicating whether the operation was successful or not.</returns>
    Result<bool> Rotate(string imagePath, string destination, int degrees, ImageType? type = null, int quality = 100);
    
    /// <summary>
    /// Trims black or white edges from an image
    /// </summary>
    /// <param name="imagePath">The path to the input image.</param>
    /// <param name="destination">The file path where the resized image will be saved.</param>
    /// <param name="fuzzPercent">the percent, 0 to 100 for the fuzziness trimming of the image</param>
    /// <param name="type">the type of the new image</param>
    /// <param name="quality">the image quality, only used by some image types</param>
    /// <returns>True if the image was trimmed, or false if stayed the same size.</returns>
    Result<bool> Trim(string imagePath, string destination, int fuzzPercent = 10, ImageType? type = null, int quality = 100);

    /// <summary>
    /// Saves an image from the image bytes
    /// </summary>
    /// <param name="imageBytes">the binary bytes of the image</param>
    /// <param name="fileNameNoExtension">the filename without the extension, the extension will be gained from the image bytes</param>
    /// <returns>the file name of the saved image</returns>
    Result<string> SaveImage(byte[] imageBytes, string fileNameNoExtension);

    /// <summary>
    /// Extracts all images from a PDF file
    /// </summary>
    /// <param name="pdf">the PDF file</param>
    /// <param name="destination">the destination to extract the images to</param>
    /// <returns>True if the operation was successful, otherwise false</returns>
    Result<bool> ExtractPdfImages(string pdf, string destination);

    /// <summary>
    /// Creates a PDF from image files
    /// </summary>
    /// <param name="pdf">the PDF file</param>
    /// <param name="images">an array of image file names to add to the PDF</param>
    /// <returns>True if the operation was successful, otherwise false</returns>
    Result<bool> CreatePdfFromImages(string pdf, string[] images);

    /// <summary>
    /// Calculates the darkness of an image on a scale of 0 to 100.
    /// </summary>
    /// <param name="imagePath">The path to the image.</param>
    /// <returns>A value between 0 and 100 indicating how dark the image is.</returns>
    Result<int> CalculateImageDarkness(string imagePath);
    
    /// <summary>
    /// Tests if a image file is a black or credits frame
    /// </summary>
    /// <param name="imageFile">the path to the image file to test</param>
    /// <returns>true if a credits or black frame</returns>
    bool IsCreditsOrBlackFrame(string imageFile);
}

/// <summary>
/// Image Options
/// </summary>
public class ImageOptions
{
    /// <summary>
    /// Gets or sets the height, 0 to maintain aspect ratio with width or no adjustment
    /// </summary>
    public int Height { get; set; }
    /// <summary>
    /// Gets or sets the width, 0 to maintain aspect ratio with height or no adjustment
    /// </summary>
    public int Width { get; set; }
    /// <summary>
    /// Gets or sets the maximum height, 0 to maintain aspect ratio with width or no adjustment
    /// </summary>
    public int MaxHeight { get; set; }
    /// <summary>
    /// Gets or sets the maximum width, 0 to maintain aspect ratio with height or no adjustment
    /// </summary>
    public int MaxWidth { get; set; }
    /// <summary>
    /// Gets or sets the quality 0 being lowest, 100 being highest
    /// </summary>
    public int? Quality { get; set; }
    /// <summary>
    /// Gets or sets the resize mode
    /// </summary>
    public ResizeMode Mode { get; set; }

    /// <summary>
    /// Gets or sets optional additional arguemtns used by ImageMagick
    /// </summary>
    public string[] AdditionalArguments { get; set; } = [];
}

/// <summary>
/// The image resize modes supported
/// </summary>
public enum ResizeMode
{
    /// <summary>
    /// Fills the image to the entire width/height specified, ignoring the aspect ratio.
    /// </summary>
    Fill = 1,

    /// <summary>
    /// Resizes the image to fit within the specified width and height while preserving its aspect ratio.
    /// If the original image's dimensions are larger than the specified width and height, it will be scaled
    /// down proportionally to fit entirely within the specified area. No additional padding is added to the image.
    /// </summary>
    Contain = 2,

    /// <summary>
    /// Resizes the image to cover the specified width/height, cropping any excess.
    /// </summary>
    Cover = 3,

    /// <summary>
    /// Resizes the image to the desired width/height, maintaining the aspect ratio, and padding the image if necessary.
    /// </summary>
    Pad = 4,

    /// <summary>
    /// Resizes the image to fit within the specified width/height, maintaining the aspect ratio, but not exceeding either dimension.
    /// </summary>
    Min = 5,

    /// <summary>
    /// Resizes the image to fit within the specified width/height, maintaining the aspect ratio, but not below either dimension.
    /// </summary>
    Max = 6
}



/// <summary>
/// Image types
/// </summary>
public enum ImageType
{
    /// <summary>
    /// JPEG image
    /// </summary>
    Jpeg,
    /// <summary>
    /// WEBP image
    /// </summary>
    Webp,
    /// <summary>
    /// GIF image
    /// </summary>
    Gif,
    /// <summary>
    /// BMP image
    /// </summary>
    Bmp,
    /// <summary>
    /// PNG image
    /// </summary>
    Png,
    /// <summary>
    /// TIFF image
    /// </summary>
    Tiff,
    /// <summary>
    /// PBM image
    /// </summary>
    Pbm,
    /// <summary>
    /// TGA image
    /// </summary>
    Tga,
    /// <summary>
    /// HEIC image
    /// </summary>
    Heic
}

/// <summary>
/// General information about an image
/// </summary>
public class ImageInfo
{
    /// <summary>
    /// Gets or sets the height of the image in pixels
    /// </summary>
    public int Height { get; init; }
    /// <summary>
    /// Gets or sets the width of the image in pixels
    /// </summary>
    public int Width { get; init; }
    /// <summary>
    /// Gets format name
    /// </summary>
    public string? Format { get; init; }
    /// <summary>
    /// Gets the image type
    /// </summary>
    public ImageType? Type { get; init; }
    /// <summary>
    /// Gets if the image is portrait, that is the height is larger than the width
    /// </summary>
    public bool IsPortrait => Width < Height;
    /// <summary>
    /// Gets if the image is landscape, that is the width is larger than the height
    /// </summary>
    public bool IsLandscape => Height < Width;
    /// <summary>
    /// Gets the date the image was taken, if this information is available
    /// </summary>
    public DateTime? DateTaken { get; init; }
}