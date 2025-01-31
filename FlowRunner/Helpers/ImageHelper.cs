using FileFlows.Plugin;
using FileFlows.Plugin.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Pbm;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ImageInfo = FileFlows.Plugin.Helpers.ImageInfo;
using isResizeMode = SixLabors.ImageSharp.Processing.ResizeMode;
using ResizeMode = FileFlows.Plugin.Helpers.ResizeMode;

namespace FileFlows.FlowRunner.Helpers;

/// <summary>
/// Image Helper
/// </summary>
public class ImageHelper : IImageHelper
{
    /// <summary>
    /// The logger to use
    /// </summary>
    private readonly ILogger Logger;
    /// <summary>
    /// The node parameters
    /// </summary>
    private readonly NodeParameters NodeParameters;
    /// <summary>
    /// The ImageMagickHelper instance
    /// </summary>
    private readonly ImageMagickHelper ImageMagick;
    
    /// <summary>
    /// Initialises a new instance of the image helper
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="args">the node parameters</param>
    public ImageHelper(ILogger logger, NodeParameters args)
    {
        Logger = logger;
        NodeParameters = args;
        ImageMagick = new(logger, args);
    }
    
    /// <summary>
    /// Initialises a new instance of the image helper
    /// </summary>
    /// <param name="logger">the logger</param>
    /// <param name="convert">The convert executable file</param>
    /// <param name="identify">The identify executable file</param>
    public ImageHelper(ILogger logger, string convert, string identify)
    {
        Logger = logger;
        ImageMagick = new(logger, convert, identify);
    }

    /// <inheritdoc />
    public Result<ImageInfo> GetInfo(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return Result<ImageInfo>.Fail("No image given");
        if(File.Exists(imagePath) == false)
            return Result<ImageInfo>.Fail($"Image '{imagePath}' does not exist");
        try
        {
            var dimensions = GetDimensions(imagePath);
            if (dimensions.Failed(out string error))
                return Result<ImageInfo>.Fail(error);

            var dateTakenResult = GetDateTaken(imagePath);

            var format = Plugin.Helpers.FileHelper.GetExtension(imagePath)?.TrimStart('.');
            ImageType? type = null;
            if (Enum.TryParse<ImageType>(format ?? string.Empty, out ImageType typeResult))
                type = typeResult;

            return new ImageInfo()
            {
                Width = dimensions.Value.Width,
                Height = dimensions.Value.Height,
                Format = format.ToUpperInvariant(),
                Type = type,
                DateTaken = dateTakenResult.IsFailed ? null : dateTakenResult.Value
            };
        }
        catch (Exception ex)
        {
            return Result<ImageInfo>.Fail("Failed to read image information: " + ex.Message);
        }
    }

    /// <summary>
    /// Gets the date the image was taken
    /// </summary>
    /// <param name="imagePath">the path to the image</param>
    /// <returns>the datetime the image was taken, or a failure result if could not be obtained</returns>
    private Result<DateTime> GetDateTaken(string imagePath)
    {
        try
        {
            string? strDateTaken = null;
            if (ImageMagick.CanUseImageMagick())
                strDateTaken = ImageMagick.GetDateTaken(imagePath);

            if (string.IsNullOrEmpty(strDateTaken))
            {
                using var image = Image.Load(imagePath);
                if (image.Metadata.ExifProfile == null)
                    return Result<DateTime>.Fail("No EXIF Profile found");
                
                if (image.Metadata.ExifProfile.TryGetValue(
                        SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.DateTimeOriginal,
                        out var dateTimeOriginalString) == false
                    || string.IsNullOrWhiteSpace(dateTimeOriginalString?.Value))
                    return Result<DateTime>.Fail("No DateTimeOriginal found");
                Logger?.ILog("No DateTimeOriginal found");
                strDateTaken = dateTimeOriginalString.Value;
            }

            if (TryParseDateTime(strDateTaken, out DateTime? dateTimeOriginal) == false || dateTimeOriginal == null)
                return Result<DateTime>.Fail("Failed to get date taken from: " + strDateTaken);
            return dateTimeOriginal.Value;

        }
        catch (Exception)
        {
            return Result<DateTime>.Fail("Could not get date taken");
        }
    }

    /// <inheritdoc />
    public void DrawRectangleOnImage(string imagePath, int x, int y, int width, int height)
    {
        // Check if the image path is empty or null
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            Logger?.WLog("Image path is empty or null.");
            return;
        }

        // Check if the image file exists
        if (File.Exists(imagePath) == false)
        {
            Logger?.WLog($"Image file does not exist: {imagePath}");
            return;
        }
        // Load the image from file
        using (var image = Image.Load<Rgb24>(imagePath))
        {
            Rectangle rectangle = new Rectangle(x, y, width, height);
            int thickness = (int)Math.Round(image.Width / 320f);

            image.Mutate(x => x.Draw(Color.Red, thickness, rectangle));
            
            // Overwrite the original image file with the modified image
            image.Save(imagePath);
        }
    }

    /// <inheritdoc />
    public Result<(int Width, int Height)> GetDimensions(string imagePath)
    {
        if (File.Exists(imagePath) == false)
            return Result<(int Width, int Height)>.Fail("Image does not exist");
        try
        {
            if (ImageMagick.CanUseImageMagick())
            {
                var result = ImageMagick.GetImageDimensions(imagePath);
                if (result.IsFailed == false)
                    return result.Value;
            }

            using var image = Image.Load<Rgb24>(imagePath);
            return (image.Width, image.Height);
        }
        catch (Exception ex)
        {
            return Result<(int Width, int Height)>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> ConvertToJpeg(string imagePath, string destination, ImageOptions? options)
        => DoConvert(imagePath, destination, ImageType.Jpeg, options);

    /// <inheritdoc />
    public Result<bool> ConvertToWebp(string imagePath, string destination, ImageOptions? options)
        => DoConvert(imagePath, destination, ImageType.Webp, options);

    private Result<bool> DoConvert(string imagePath, string destination, ImageType type, ImageOptions? options)
    {
        if (ValidatePaths(imagePath, destination).Failed(out string error))
            return Result<bool>.Fail(error);
        try
        {
            if (ImageMagick.CanUseImageMagick())
            {
                var result = ImageMagick.ConvertImage(imagePath, destination, options);
                if (result.IsFailed == false)
                    return true;
            }

            using var image = Image.Load(imagePath);
            
            if (options != null)
            {
                (int newWidth, int newHeight) = CalculateNewDimensions(image.Width, image.Height, options);

                if (newWidth != image.Width || newHeight != image.Height)
                {
                    // Resize the image
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(newWidth, newHeight),
                        Mode = isResizeMode.Max
                    }));
                }
            }

            IImageEncoder encoder;
            switch (type)
            {
                case ImageType.Jpeg:
                    encoder = new JpegEncoder()
                    {
                        Quality = options?.Quality ?? 75
                    };
                    break;
                case ImageType.Webp:
                    encoder = new WebpEncoder()
                    {
                        Quality = options?.Quality ?? 75
                    };
                    break;
                default:
                    throw new ArgumentException("Unsupported image type");
            }
            
            image.Save(destination, encoder);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> ConvertImage(string imagePath, string destination, ImageType type, int quality = 100)
    {
        if (ValidatePaths(imagePath, destination).Failed(out string error))
            return Result<bool>.Fail(error);
        try
        {
            if (ImageMagick.CanUseImageMagick())
            {
                Logger.ILog($"Converting using ImageMagick to {type}");
                var result = ImageMagick.ConvertImage(imagePath, destination, new ImageOptions()
                {
                    Quality = quality
                });
                if (result.IsFailed == false)
                    return true;
            }

            var encoderResult = GetImageEncoder(type, quality);
            if (encoderResult.Failed(out error))
                return Result<bool>.Fail(error);
            
            using var image = Image.Load(imagePath);
            image.Save(destination, encoderResult.Value);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Validatest he paths are valid
    /// </summary>
    /// <param name="imagePath">the image path</param>
    /// <param name="destination">the file to save the new image to</param>
    /// <returns>true if valid,otherwise a failure result</returns>
    private Result<bool> ValidatePaths(string imagePath, string destination)
    {
        if (File.Exists(imagePath) == false)
            return Result<bool>.Fail("Image does not exist");
        var destFileInfo = new FileInfo(destination);
        if (destFileInfo.Directory.Exists == false)
            NodeParameters.CreateDirectoryIfNotExists(destFileInfo.Directory.FullName);
        return true;
    }

    /// <inheritdoc />
    public Result<bool> Resize(string imagePath, string destination, int width, int height, ResizeMode mode, ImageType? type = null, int quality = 100)
    {
        if (ValidatePaths(imagePath, destination).Failed(out string error))
            return Result<bool>.Fail(error);
        
        // Validate input size
        if ((width == 0 && height == 0) || width < 0 || height < 0)
            return Result<bool>.Fail("Width and height must be positive values, or one dimension must be 0 to maintain aspect ratio.");
        
        try
        {
            if (ImageMagick.CanUseImageMagick())
            {
                Logger.ILog("Using ImageMagick for resizing");
                var result = ImageMagick.ConvertImage(imagePath,destination, new ()
                {
                    Width = width,
                    Height = height,
                    Mode = mode,
                    Quality = quality
                });
                if (result.IsFailed == false)
                    return true;
                Logger.ILog("Failed to resize using ImageMagick, falling back to ImageSharp");
            }
            else
            {
                Logger.ILog("Using ImageSharp for resizing");
            }
            
            using var image = Image.Load(imagePath);
            
            image.Mutate(x => x.Resize(width, height));
            if (type != null)
            {
                var encoderResult = GetImageEncoder(type.Value, quality);
                if (encoderResult.Failed(out error))
                    return Result<bool>.Fail(error);
                image.Save(destination, encoderResult.Value);
            }
            else
            {
                image.Save(destination);
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            // Return failure with the error message
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> FlipVertically(string imagePath, string destination, ImageType? type = null, int quality = 100)
        => DoFlip(imagePath, destination, type, quality, vertically: true);

    /// <inheritdoc />
    public Result<bool> FlipHorizontally(string imagePath, string destination, ImageType? type = null, int quality = 100)
        => DoFlip(imagePath, destination, type, quality, vertically: false);

    /// <summary>
    /// Flips an image
    /// </summary>
    /// <param name="imagePath">The path to the input image.</param>
    /// <param name="destination">The file path where the resized image will be saved.</param>
    /// <param name="type">the image type of the destination file</param>
    /// <param name="quality">the image quality, only used by some image types</param>
    /// <param name="vertically">true for vertically, otherwise false for horizontally</param>
    /// <returns>A result indicating whether the operation was successful or not.</returns>
    private Result<bool> DoFlip(string imagePath, string destination, ImageType? type, int quality, bool vertically)
    {
        if (ValidatePaths(imagePath, destination).Failed(out string error))
            return Result<bool>.Fail(error);
        
        try
        {
            if (ImageMagick.CanUseImageMagick())
            {
                var result = ImageMagick.FlipImage(imagePath, destination, quality, vertically: vertically);
                if (result.IsFailed == false)
                    return true;
            }
            
            using var image = Image.Load(imagePath);
            
            image.Mutate(x => x.Flip(vertically ? FlipMode.Vertical : FlipMode.Horizontal));
            if (type != null)
            {
                var encoderResult = GetImageEncoder(type.Value, quality);
                if (encoderResult.Failed(out error))
                    return Result<bool>.Fail(error);
                image.Save(destination, encoderResult.Value);
            }
            else
            {
                image.Save(destination);
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            // Return failure with the error message
            return Result<bool>.Fail(ex.Message);
        }
        
    }

    /// <inheritdoc />
    public Result<bool> Rotate(string imagePath, string destination, int degrees, ImageType? type = null, int quality = 100)
    {
        if (ValidatePaths(imagePath, destination).Failed(out string error))
            return Result<bool>.Fail(error);
        
        try
        {
            if (ImageMagick.CanUseImageMagick())
            {
                var result = ImageMagick.Rotate(imagePath, destination, degrees, quality);
                if (result.IsFailed == false)
                    return true;
            }
            
            using var image = Image.Load(imagePath);
            
            image.Mutate(x => x.Rotate(degrees));
            if (type != null)
            {
                var encoderResult = GetImageEncoder(type.Value, quality);
                if (encoderResult.Failed(out error))
                    return Result<bool>.Fail(error);
                image.Save(destination, encoderResult.Value);
            }
            else
            {
                image.Save(destination);
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            // Return failure with the error message
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> Trim(string imagePath, string destination, int fuzzPercent, ImageType? type = null, int quality = 100)
    {
        if (ValidatePaths(imagePath, destination).Failed(out string error))
            return Result<bool>.Fail(error);
        
        try
        {
            if (ImageMagick.CanUseImageMagick())
            {
                var result = ImageMagick.Trim(imagePath, destination, fuzzPercent, quality);
                if (result.IsFailed == false)
                    return true;
            }
            
            Logger?.WLog("ImageMagick required for trimming images");
            return false;
        }
        catch (Exception ex)
        {
            // Return failure with the error message
            return Result<bool>.Fail(ex.Message);
        }
    }
    
    /// <inheritdoc />
    public Result<string> SaveImage(byte[] imageBytes, string fileNameNoExtension)
    {
        if (imageBytes?.Any() != true)
            return Result<string>.Fail("No image bytes");
        if (string.IsNullOrWhiteSpace(fileNameNoExtension))
            return Result<string>.Fail("No file name");
        try
        {
            using var image = Image.Load(imageBytes);

            // Infer the image format
            (IImageFormat? imageFormat, string? fileExtension) = InferImageFormat(imageBytes);
            if (imageFormat == null)
            {
                Logger?.WLog("Failed to inter image type from PDF, failing back to JPG");
                imageFormat = JpegFormat.Instance;
                fileExtension = "jpg";
            }
            else
            {
                Logger?.ILog("File Extension of image: " + fileExtension);
            }

            var file = fileNameNoExtension.TrimEnd('.') + "." + fileExtension;
            var fileInfo = new FileInfo(file);
            
            if (fileInfo.Directory.Exists == false)
                fileInfo.Directory.Create();

            using var outputStream = File.Create(file);
            image.Save(outputStream, imageFormat);
            return file;
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> ExtractPdfImages(string pdf, string destination)
    {
        if (File.Exists(pdf) == false)
            return Result<bool>.Fail("PDF does not exist");
        
        if (Directory.Exists(destination) == false)
            NodeParameters.CreateDirectoryIfNotExists(destination);

        try
        {
            if (ImageMagick.CanUseImageMagick() == false)
                return Result<bool>.Fail("ImageMagick required for extract PDF images");
            
            return ImageMagick.ExtractPdfImages(pdf, destination);
        }
        catch (Exception ex)
        {
            // Return failure with the error message
            return Result<bool>.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public Result<bool> CreatePdfFromImages(string pdf, string[] images)
    {
        var pdfFile = new FileInfo(pdf);
        if(pdfFile.Directory.Exists == false)
            NodeParameters.CreateDirectoryIfNotExists(pdfFile.Directory.FullName);

        try
        {
            if (ImageMagick.CanUseImageMagick() == false)
                return Result<bool>.Fail("ImageMagick required for creating a PDF from images");
            
            return ImageMagick.CreatePdfFromImages(pdf, images);
        }
        catch (Exception ex)
        {
            // Return failure with the error message
            return Result<bool>.Fail(ex.Message);
        }
    }


    /// <summary>
    /// Infers the image format based on the first few bytes of the image data.
    /// </summary>
    /// <param name="bytes">The image data bytes.</param>
    /// <returns>The inferred image format and file extension.</returns>
    private static (IImageFormat? Format, string? Extension) InferImageFormat(byte[] bytes)
    {
        // Try to infer image format based on magic numbers
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8) // JPEG
            return (JpegFormat.Instance, "jpg");
        if (bytes.Length >= 8 && BitConverter.ToUInt64(bytes, 0) == 0x89504E470D0A1A0A) // PNG
            return (PngFormat.Instance, "png");
        if (bytes.Length >= 4 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38) // GIF
            return (GifFormat.Instance, "gif");
        if (bytes.Length >= 4 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50) // WebP
            return (WebpFormat.Instance, "webp");
        if (bytes.Length >= 4 && BitConverter.ToUInt32(bytes, 0) == 0x49492A00) // TIFF
            return (TiffFormat.Instance, "tiff");
        if (bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D) // BMP
            return (BmpFormat.Instance, "bmp");

        // If none of the known formats are detected, fall back to Image.DetectFormat()
        try
        {
            IImageFormat format = Image.DetectFormat(bytes);
            string extension = format?.DefaultMimeType?.Split('/')[1] ?? "png";
            return (format, extension);
        }
        catch (Exception)
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Calculates the new dimensions for resizing an image based on the provided options.
    /// </summary>
    /// <param name="width">The original width of the image.</param>
    /// <param name="height">The original height of the image.</param>
    /// <param name="options">The options specifying the desired dimensions or constraints for resizing.</param>
    /// <returns>A tuple containing the new width and height for the resized image.</returns>
    internal static (int Width, int Height) CalculateNewDimensions(int width, int height, ImageOptions? options)
    {
        int newWidth = width;
        int newHeight = height;
        
        if (options == null)
            return (width, height);

        // Calculate new dimensions based on options
        if (options is { Width: > 0, Height: > 0 })
        {
            // Both width and height are specified, use them directly
            newWidth = options.Width;
            newHeight = options.Height;
        }
        else if (options.Width > 0)
        {
            // Only width is specified, scale height proportionally
            newWidth = options.Width;
            newHeight = (int)Math.Round((double)height / width * options.Width);
        }
        else if (options.Height > 0)
        {
            // Only height is specified, scale width proportionally
            newWidth = (int)Math.Round((double)width / height * options.Height);
            newHeight = options.Height;
        }
        else if (options.MaxWidth > 0 && options.MaxHeight > 0)
        {
            // Both max width and max height are specified, scale the image down to fit within the bounds
            double widthRatio = (double)width / options.MaxWidth;
            double heightRatio = (double)height / options.MaxHeight;
            double maxRatio = Math.Max(widthRatio, heightRatio);

            newWidth = (int)Math.Round(width / maxRatio);
            newHeight = (int)Math.Round(height / maxRatio);
        }
        else if (options.MaxWidth > 0)
        {
            // Only max width is specified, scale the image down to fit within the width
            double ratio = (double)width / options.MaxWidth;
            newWidth = options.MaxWidth;
            newHeight = (int)Math.Round(height / ratio);
        }
        else if (options.MaxHeight > 0)
        {
            // Only max height is specified, scale the image down to fit within the height
            double ratio = (double)height / options.MaxHeight;
            newWidth = (int)Math.Round(width / ratio);
            newHeight = options.MaxHeight;
        }

        return (newWidth, newHeight);
    }


    /// <summary>
    /// Gets the image format from its type
    /// </summary>
    /// <param name="type">the type</param>
    /// <returns>the image format</returns>
    private Result<IImageFormat> GetImageFormat(ImageType type)
    {
        switch (type)
        {
            case ImageType.Jpeg: return JpegFormat.Instance;
            case ImageType.Webp: return WebpFormat.Instance;
            case ImageType.Gif: return GifFormat.Instance;
            case ImageType.Bmp: return BmpFormat.Instance;
            case ImageType.Png: return PngFormat.Instance;
            case ImageType.Tiff: return TiffFormat.Instance;
        }

        return Result<IImageFormat>.Fail("Not supported");
    }

    /// <summary>
    /// Gets the image encoder from its type
    /// </summary>
    /// <param name="type">the type</param>
    /// <param name="quality">the image quality</param>
    /// <returns>the image encoder</returns>
    private Result<IImageEncoder> GetImageEncoder(ImageType type, int quality)
    {
        switch (type)
        {
            case ImageType.Jpeg: return new JpegEncoder { Quality = quality };
            case ImageType.Webp: return new WebpEncoder { Quality = quality };
            case ImageType.Gif: return new GifEncoder();
            case ImageType.Bmp: return new GifEncoder();
            case ImageType.Png: return new GifEncoder();
            case ImageType.Tiff: return new TiffEncoder();
            case ImageType.Pbm: return new PbmEncoder();
            case ImageType.Tga: return new TgaEncoder();
        }

        return Result<IImageEncoder>.Fail("Not supported");
    }


    /// <summary>
    /// Tries to parse a DateTime from a string, attempting different formats.
    /// </summary>
    /// <param name="dateTimeString">The string representation of the DateTime.</param>
    /// <param name="dateTime">When this method returns, contains the parsed DateTime if successful; otherwise, null.</param>
    /// <returns>
    /// True if the parsing was successful; otherwise, false.
    /// </returns>
    static bool TryParseDateTime(string dateTimeString, out DateTime? dateTime)
    {
        DateTime parsedDateTime;

        // Try parsing using DateTime.TryParse
        if (DateTime.TryParse(dateTimeString, out parsedDateTime))
        {
            dateTime = parsedDateTime;
            return true;
        }

        // Define an array of possible date formats for additional attempts
        string[] dateFormats = { "yyyy:MM:dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss" /* Add more formats if needed */ };

        // Attempt to parse using different formats
        foreach (var format in dateFormats)
        {
            if (DateTime.TryParseExact(dateTimeString, format, null, System.Globalization.DateTimeStyles.None,
                    out parsedDateTime))
            {
                dateTime = parsedDateTime;
                return true;
            }
        }

        // Set dateTime to null if parsing fails with all formats
        dateTime = null;
        return false;
    }
    
    /// <summary>
    /// Calculates the darkness of an image on a scale of 0 to 100.
    /// </summary>
    /// <param name="imagePath">The path to the image.</param>
    /// <returns>A value between 0 and 100 indicating how dark the image is.</returns>
    public Result<int> CalculateImageDarkness(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return Result<int>.Fail("No image given");
        if (File.Exists(imagePath) == false)
            return Result<int>.Fail($"Image '{imagePath}' does not exist");

        try
        {
            using var image = Image.Load<Rgb24>(imagePath);
            long totalBrightness = 0;
            int pixelCount = image.Width * image.Height;

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        var pixel = row[x];
                        // Calculate brightness as the average of the RGB components
                        int brightness = (pixel.R + pixel.G + pixel.B) / 3;
                        totalBrightness += brightness;
                    }
                }
            });

            // Calculate average brightness
            double averageBrightness = totalBrightness / (double)pixelCount;
            // Scale to 0-100 where 0 is completely black and 100 is completely white
            int darkness = (int)((255 - averageBrightness) / 255 * 100);

            return Result<int>.Success(darkness);
        }
        catch (Exception ex)
        {
            return Result<int>.Fail("Failed to calculate image darkness: " + ex.Message);
        }
    }
}