﻿using BarClip.Data.Schema;
using FFMpegCore;
using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using FFMpegCore.Enums;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp.Processing;
using System.Collections.Concurrent;
using BarClip.Models.Domain;
using BarClip.Models.Options;
using Microsoft.Extensions.Options;
using BarClip.Models.Requests;

namespace BarClip.Core.Services;

public class FrameService
{
    private readonly string _onnxModelPath;


    public FrameService(IOptions<OnnxModelOptions> options)
    {
        _onnxModelPath = Path.Combine(AppContext.BaseDirectory, options.Value.Path);
    }
    public async Task<List<Frame>> ExtractFrames(OriginalVideoRequest originalVideo) // medium
    {
        string tempFramePath = Path.Combine(Path.GetTempPath(), "frames");
        Directory.CreateDirectory(tempFramePath);

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
                    .WithCustomArgument("-vf fps=1 -q:v 5")
                    .WithCustomArgument("-threads 0"))
                .ProcessAsynchronously();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error extracting frames: {ex.Message}");
        }

        var frames = CreateFramesFromPath(tempFramePath); // long
        return frames;
    }

    public List<Frame> CreateFramesFromPath(string tempFramePath)
    {
        var session = new InferenceSession(_onnxModelPath);

        var frames = new ConcurrentBag<Frame>();

        var files = Directory.GetFiles(tempFramePath, "frame_*.png")
                             .OrderBy(GetFrameNumber)
                             .ToList();

        Parallel.ForEach(files, file =>
        {
            try
            {
                var frame = new Frame
                {
                    FilePath = file,
                    InputValue = ConvertFrameToOnnxValue(file)
                };
                frame.PlateDetections = PlateDetectionService.GetDetections(frame, session);

                frames.Add(frame);
            }
            finally
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
        });

        var orderedFrames = frames.OrderBy(f => GetFrameNumber(f.FilePath))
                                  .Select((f, index) => { f.FrameNumber = index; return f; })
                                  .ToList();

        if (Directory.Exists(tempFramePath))
        {
            Directory.Delete(tempFramePath, true);
        }

        return orderedFrames;
    }

    private NamedOnnxValue ConvertFrameToOnnxValue(string framePath)
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

    private int GetFrameNumber(string path)
    {
        string fileName = Path.GetFileNameWithoutExtension(path);
        var parts = fileName.Split('_');
        return int.TryParse(parts.Last(), out int n) ? n : int.MaxValue;
    }
}