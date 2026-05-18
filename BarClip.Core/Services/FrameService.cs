using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp;
using System.Collections.Concurrent;
using BarClip.Models.Domain;
using BarClip.Models.Options;
using Microsoft.Extensions.Options;
using BarClip.Models.Requests;
using BarClip.Core.Helpers;
using BarClip.Core.Interfaces;

namespace BarClip.Core.Services;

public class FrameService
{
    private readonly string _onnxModelPath;


    public FrameService(IOptions<OnnxModelOptions> options)
    {
        _onnxModelPath = options.Value.Path;
    }

    //public async Task<List<Frame>> ExtractAndProcessFrames(OriginalVideoRequest originalVideo) // medium
    //{
    //    string tempFramePath = Path.Combine(Path.GetTempPath(), "frames");
    //    Directory.CreateDirectory(tempFramePath);

    //    try
    //    {
    //        await _videoEditor.ExtractAllFramesAsync(originalVideo, tempFramePath);
    //    }
    //    catch (Exception ex)
    //    {
    //        throw new Exception($"Error extracting frames: {ex.Message}");
    //    }

    //    var frames = ProcessFrames(tempFramePath, originalVideo.LifterFilter); // long

    //    return frames;
    //}

    public async Task<List<Frame>> ProcessFrames(string tempFramePath, LifterFilter lifterFilter, IProgress<double> progress = null)
    {
        var session = new InferenceSession(_onnxModelPath);

        var frames = new ConcurrentBag<Frame>();

        var files = Directory.GetFiles(tempFramePath, "frame_*.png")
                             .OrderBy(FrameHelper.GetFrameNumber)
                             .ToList();

        int processed = 0;
        double weight = .4;

        await Parallel.ForEachAsync(files, async (file, ct) =>
        {
            try
            {
                var preparedFrame = FrameHelper.PrepareFrame(file, lifterFilter);
                var inputValue = OnnxHelper.ConvertToOnnxValue(preparedFrame);
                var frame = new Frame
                {
                    FilePath = file,
                    InputValue = inputValue
                };

                frame.PlateDetections = OnnxHelper.GetDetections(frame, session);
                frames.Add(frame);
            }
            finally
            {
                if (File.Exists(file))
                    File.Delete(file);

                var count = Interlocked.Increment(ref processed);
                progress?.Report((double)count / files.Count);
            }
        });

        var orderedFrames = frames.OrderBy(f => FrameHelper.GetFrameNumber(f.FilePath))
                                  .Select((f, index) => { f.FrameNumber = index; return f; })
                                  .ToList();

        if (Directory.Exists(tempFramePath))
        {
            Directory.Delete(tempFramePath, true);
        }

        return orderedFrames;
    }

}