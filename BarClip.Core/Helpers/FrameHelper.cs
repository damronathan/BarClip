using BarClip.Models.Requests;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BarClip.Core.Helpers;
public class FrameHelper
{
    //public static Image<Rgb24> PrepareFrame(string framePath, LifterFilter lifterFilter)
    //{
    //    using var image = Image.Load<Rgb24>(framePath);
    //    int targetSize = 640;

    //    // Target aspect ratio from ffprobe (DAR 259:360)
    //    float targetAspect = 259f / 360f;

    //    if (lifterFilter == LifterFilter.Left)
    //    {
    //        // ---- Left: zoom to aspect ratio ----
    //        int cropHeight = image.Height;
    //        int cropWidth = (int)(cropHeight * targetAspect);
    //        if (cropWidth > image.Width)
    //            cropWidth = image.Width; // safety

    //        var cropRect = new Rectangle(0, 0, cropWidth, cropHeight);
    //        image.Mutate(ctx => ctx.Crop(cropRect));
    //    }
    //    else if (lifterFilter == LifterFilter.Right)
    //    {
    //        // ---- Right: keep whole frame, black out left strip ----
    //        int stripWidth = (int)(image.Height * targetAspect);
    //        if (stripWidth > image.Width) stripWidth = image.Width;

    //        // Draw black rectangle over left part
    //        var blackout = new Rectangle(0, 0, stripWidth, image.Height);
    //        image.Mutate(ctx => ctx.Fill(Color.Black, blackout));
    //    }
    //    // else: no filter = keep whole frame

    //    // Resize to fit target size
    //    float ratio = Math.Min((float)targetSize / image.Width, (float)targetSize / image.Height);
    //    int newWidth = (int)(image.Width * ratio);
    //    int newHeight = (int)(image.Height * ratio);
    //    image.Mutate(ctx => ctx.Resize(newWidth, newHeight));

    //    // Pad into 640x640
    //    var paddedImage = new Image<Rgb24>(targetSize, targetSize);
    //    int padX = (targetSize - newWidth) / 2;
    //    int padY = (targetSize - newHeight) / 2;
    //    paddedImage.Mutate(ctx => ctx.DrawImage(image, new Point(padX, padY), 1f));

    //    return paddedImage;
    //}


    public static Image<Rgb24> PrepareFrame(string framePath, LifterFilter lifterFilter)
    {
        using var image = Image.Load<Rgb24>(framePath);
        int targetSize = 640;

        // Determine crop based on lifter filter
        Rectangle cropRect = lifterFilter switch
        {
            LifterFilter.Right => new Rectangle(image.Width / 2, 0, image.Width / 2, image.Height),
            LifterFilter.Left => new Rectangle(0, 0, image.Width / 2, image.Height),
            _ => new Rectangle(0, 0, image.Width, image.Height)
        };

        // Crop to selected area
        image.Mutate(ctx => ctx.Crop(cropRect));

        // Resize to fit target size
        float ratio = Math.Min((float)targetSize / image.Width, (float)targetSize / image.Height);
        int newWidth = (int)(image.Width * ratio);
        int newHeight = (int)(image.Height * ratio);

        image.Mutate(ctx => ctx.Resize(newWidth, newHeight));

        // Create padded 640x640 image
        var paddedImage = new Image<Rgb24>(targetSize, targetSize);
        int padX = (targetSize - newWidth) / 2;
        int padY = (targetSize - newHeight) / 2;

        paddedImage.Mutate(ctx => ctx.DrawImage(image, new Point(padX, padY), 1f));

        return paddedImage;
    }


    public static int GetFrameNumber(string path)
    {
        string fileName = Path.GetFileNameWithoutExtension(path);
        var parts = fileName.Split('_');
        return int.TryParse(parts.Last(), out int n) ? n : int.MaxValue;
    }
}
