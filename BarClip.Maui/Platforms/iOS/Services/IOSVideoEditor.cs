using BarClip.Core.Interfaces;
using BarClip.Core.Services;
using BarClip.Maui.Platforms.iOS.Helpers;
using BarClip.Models.Requests;
using FFMpegCore.Helpers;
using static BarClip.Core.Helpers.FileHelper;

namespace BarClip.Maui.Platforms.iOS.Services;

public class IOSVideoEditor : IVideoEditor
{
    private readonly FrameService _frameService;
    private readonly PlateAnalysisService _plateAnalysisService;

    public IOSVideoEditor(FrameService frameService, PlateAnalysisService plateAnalysisService)
    {
        _frameService = frameService;
        _plateAnalysisService = plateAnalysisService;
    }
    public async Task<ProcessedVideoRequest> ProcessVideo(SessionFolderPaths sessionFolderPaths, OriginalVideoRequest video)
    {
        video.Frames = await ExtractAndProcessFrames(video);

        if (video.TrimStart == TimeSpan.Zero)
        {
            _plateAnalysisService.SetTrim(video);
        }

        ProcessedVideoRequest processedVideo = new()
        {
            Id = Guid.NewGuid(),
            FilePath = Path.Combine(sessionFolderPaths.Processed, $"{video.LiftNumber}_Trimmed.MOV"),
            Duration = video.TrimFinish - video.TrimStart
        };

        try
        {
            string? weightText = null;
            if (video.WeightKg is not null)
            {
                var lbWeight = Math.Floor((decimal)(video.WeightKg * 2.2045));
                weightText = $"{video.WeightKg}KG/{lbWeight}LB";
            }
            await AVFoundationHelper.TrimAndLabelAsync(video, processedVideo, weightText);

            return processedVideo;

        }
        catch (Exception ex)
        {
            throw new Exception("Error during trimming and labeling: " + ex.Message);
        }
    }
    public async Task<List<BarClip.Models.Domain.Frame>> ExtractAndProcessFrames(OriginalVideoRequest originalVideo) // medium
    {
        string tempFramePath = Path.Combine(Path.GetTempPath(), "frames");
        Directory.CreateDirectory(tempFramePath);

        try
        {
            await AVFoundationHelper.ExtractAllFramesAsync(originalVideo, tempFramePath);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error extracting frames: {ex.Message}");
        }

        var frames = _frameService.ProcessFrames(tempFramePath, originalVideo.LifterFilter); // long

        return frames;
    }
    public async Task<string> MergeVideos(SessionFolderPaths sessionFolderPaths, Guid sessionId)
    {
        return await AVFoundationHelper.MergeVideos(sessionFolderPaths, sessionId);
    }
    public async Task<string[]> ExtractThumbnails(string originalFolderPath, string thumbnailFolderPath)
    {
        return await AVFoundationHelper.ExtractThumbnails(originalFolderPath, thumbnailFolderPath);
    }
}
