using BarClip.Data.Schema;
using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using Xabe.FFmpeg;
using SixLabors.ImageSharp.Processing;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace BarClip.Core.Services;

public class FrameService
{
    public async static Task<List<Frame>> ExtractFrames(Video originalVideo)
    {
        IVideoStream videoStream = originalVideo.VideoInfo.VideoStreams.First().SetCodec(VideoCodec.png);

        string tempFramePath = Path.Combine(Path.GetTempPath(), "frames");

        string outputFileNameBuilder(string number) => Path.Combine(tempFramePath, $"frame_{number}.png");

        int fps = (int)Math.Round(videoStream.Framerate);

        try
        {
            await FFmpeg.Conversions.New()
                .AddStream(videoStream)
                .ExtractEveryNthFrame(fps, outputFileNameBuilder)
                .Start();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error extracting frames: {ex.Message}");
        }
        var frames = CreateFramesFromPath(tempFramePath);

        return frames;
    }
    public static List<Frame> CreateFramesFromPath(string tempFramePath)
    {
        var frames = new List<Frame>();
        int frameNumber = 0;
        foreach (var file in Directory.GetFiles(tempFramePath, "frame_*.png").OrderBy(GetFrameNumber))
        {
            try
            {
                var frame = new Frame
                {
                    FrameNumber = frameNumber,
                    FilePath = file,
                    InputValue = ConvertFrameToOnnxValue(file)
                };

                frame.PlateDetections = PlateDetectionService.GetDetections(frame);

                frameNumber++;

                frames.Add(frame);

            }
            finally
            {
                File.Delete(file);
            }
        }
        return frames;
    }
    private static NamedOnnxValue ConvertFrameToOnnxValue(string framePath)
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

        paddedImage.Mutate(ctx => ctx.DrawImage(image, new Point(padX, padY), 1f));

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
    private static int GetFrameNumber(string path)
    {
        string fileName = Path.GetFileNameWithoutExtension(path);
        var parts = fileName.Split('_');
        return int.TryParse(parts.Last(), out int n) ? n : int.MaxValue;
    }
}
