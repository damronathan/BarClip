using BarClip.Core.Services;
using BarClip.Data.Schema;
using FFMpegCore;
using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using FFMpegCore.Enums;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp.Processing;
using OpenCvSharp;
using OpenCvSharp.Tracking;

public class FrameService
{
    public async static Task<List<Frame>> ExtractFrames(Video originalVideo) // medium
    {
        string tempFramePath = Path.Combine(Path.GetTempPath(), "frames");
        Directory.CreateDirectory(tempFramePath);

        // Get video info to determine framerate
        var videoStream = originalVideo.VideoAnalysis.VideoStreams.FirstOrDefault();
        if (videoStream == null)
            throw new Exception("No video stream found");

        double fps = videoStream.FrameRate;

        try
        {
            await FFMpegArguments
                .FromFileInput(originalVideo.FilePath)
                .OutputToFile(Path.Combine(tempFramePath, "frame_%d.png"), overwrite: true, options => options
                    .WithVideoFilters(filterOptions => filterOptions
                        .Scale(VideoSize.Original))
                    .WithCustomArgument("-vf fps=1 -q:v 5"))
                .ProcessAsynchronously();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error extracting frames: {ex.Message}");
        }

        var frames = CreateFramesFromPath(tempFramePath); // long
        return frames;
    }

    public static List<Frame> CreateFramesFromPath(string tempFramePath)
    {
        var session = new InferenceSession(@"C:\Users\19139\runs\detect\train\weights\best.onnx");
        var tracker = TrackerCSRT.Create();

        var frames = new List<Frame>();

        var files = Directory.GetFiles(tempFramePath, "frame_*.png")
                             .OrderBy(GetFrameNumber)
                             .ToList();

        foreach ( var file in files)
        {
            var frame = new Frame
            {
                FilePath = file,
            };
            frames.Add(frame);
        }
        int n = 5;
        int i = 0;
        PlateDetection previousDetection = null;
        var previousFrame = new Mat();

        while ( i < frames.Count)
        {
            var frame = frames[i];

            if (i % n == 0)
            {
                frame.InputValue = ConvertFrameToOnnxValue(frame.FilePath);
                frame.PlateDetections = PlateDetectionService.GetDetections(frame, session);
                previousDetection = TrimService.SelectBestDetection(frame, previousDetection);
                frame.MatValue = ConvertFrameToMat(frame.FilePath);
                previousFrame = frame.MatValue;
            }
            else
            {
                frame.PlateDetections = new List<PlateDetection>();
                var previousBox = new Rect((int)previousDetection.X, (int)previousDetection.Y, (int)previousDetection.Width, (int)previousDetection.Height);
                var currentImage = ConvertFrameToMat(frame.FilePath);
                frame.MatValue = ConvertFrameToMat(frame.FilePath);
                previousDetection = PlateDetectionService.GetTracker(tracker, previousFrame, previousBox, frame);
                frame.PlateDetections.Add(previousDetection);
            }

            i++;
        }

        // After all frames processed, assign frame numbers in correct order:
        var orderedFrames = frames.OrderBy(f => GetFrameNumber(f.FilePath))
                                  .Select((f, index) => { f.FrameNumber = index; return f; })
                                  .ToList();

        // Clean up temp directory
        if (Directory.Exists(tempFramePath))
        {
            foreach(var file in files)
            {
                File.Delete(file);
            }
            Directory.Delete(tempFramePath, true);
        }

        return orderedFrames;
    }

    private static NamedOnnxValue ConvertFrameToOnnxValue(string framePath)
    {
        var paddedImage = ResizeAndPad(framePath);

        int width = paddedImage.Width;
        int height = paddedImage.Height;
        float[] data = new float[3 * width * height];
        int channelSize = width * height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Rgb24 pixel = paddedImage[x, y];
                int idx = y * width + x;
                data[idx] = pixel.R / 255f;
                data[channelSize + idx] = pixel.G / 255f;
                data[2 * channelSize + idx] = pixel.B / 255f;
            }
        }

        var inputTensor = new DenseTensor<float>(data, [1, 3, height, width]);
        return NamedOnnxValue.CreateFromTensor("images", inputTensor);
    }

    public static Image<Rgb24> ResizeAndPad(string framePath)
    {
        using var image = Image.Load<Rgb24>(framePath);
        int targetSize = 640;
        float ratio = Math.Min((float)targetSize / image.Width, (float)targetSize / image.Height);
        int newWidth = (int)(image.Width * ratio);
        int newHeight = (int)(image.Height * ratio);
        image.Mutate(ctx => ctx.Resize(newWidth, newHeight));

        var paddedImage = new Image<Rgb24>(targetSize, targetSize);
        int padX = (targetSize - newWidth) / 2;
        int padY = (targetSize - newHeight) / 2;
        paddedImage.Mutate(ctx => ctx.DrawImage(image, new SixLabors.ImageSharp.Point(padX, padY), 1f));

        return paddedImage.Clone();
    }

    public static Mat ConvertFrameToMat(string framePath)
    {
        var paddedImage = ResizeAndPad(framePath);

        using var ms = new MemoryStream();
        paddedImage.SaveAsPng(ms);
        ms.Seek(0, SeekOrigin.Begin);

        Mat mat = Mat.FromStream(ms, ImreadModes.Color);

        return mat;
    }


    private static int GetFrameNumber(string path)
    {
        string fileName = Path.GetFileNameWithoutExtension(path);
        var parts = fileName.Split('_');
        return int.TryParse(parts.Last(), out int n) ? n : int.MaxValue;
    }
}