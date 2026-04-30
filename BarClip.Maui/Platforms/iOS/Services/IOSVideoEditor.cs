using BarClip.Core.Interfaces;
using BarClip.Core.Services;
using BarClip.Maui.Platforms.iOS.Helpers;
using BarClip.Models.Requests;
using static BarClip.Core.Helpers.FileHelper;

namespace BarClip.Maui.Platforms.iOS.Services;

public class IOSVideoEditor : IVideoEditor
{
    private readonly FrameService _frameService;
    private readonly PlateAnalysisService _plateAnalysisService;
    private readonly VideoPickerService _videoPickerService;

    public IOSVideoEditor(FrameService frameService, PlateAnalysisService plateAnalysisService, VideoPickerService videoPickerService)
    {
        _frameService = frameService;
        _plateAnalysisService = plateAnalysisService;
        _videoPickerService = videoPickerService;
    }
    public async Task<ProcessedVideoRequest> ProcessVideo(SessionFolderPaths sessionFolderPaths, OriginalVideoRequest video)
    {
        video.Duration = await AVFoundationHelper.GetDurationAsync(video.CompressedPath);
        try
        {
            video.Frames = await ExtractAndProcessFrames(video);
        }
        catch (Exception ex)
        {
            throw new Exception("Error during frame extraction: ", ex);
        }

        if (video.TrimStart == TimeSpan.Zero)
        {
            try
            {

                _plateAnalysisService.SetTrim(video);

            }
            catch (Exception ex)
            {
                throw new Exception("Error while setting trim: ", ex);
            }
        }

        try
        {
            ProcessedVideoRequest processedVideo = new()
            {
                Id = Guid.NewGuid(),
                FilePath = Path.Combine(sessionFolderPaths.Processed, $"{video.LiftNumber}_Trimmed.MOV"),
                Duration = video.TrimFinish - video.TrimStart
            };
            string? weightText = null;
            if (video.WeightKg is not null)
            {
                var lbWeight = Math.Floor((decimal)(video.WeightKg * 2.2045));
                weightText = $"{video.WeightKg}KG/{lbWeight}LB";
            }
            await AVFoundationHelper.TrimAsync(video, processedVideo);
            return processedVideo;

        }
        catch (Exception ex)
        {
            throw new Exception("Error during video processing: ", ex);
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
            throw new Exception("Error extracting frames: ", ex);
        }

        try
        {
            var frames = _frameService.ProcessFrames(tempFramePath, originalVideo.LifterFilter);
            return frames;
        }
        catch (Exception ex)
        {
            throw new Exception("Error while processing frames: ", ex);
        }
    }
    public async Task<string> MergeVideos(SessionFolderPaths sessionFolderPaths, Guid sessionId)
    {
        return await AVFoundationHelper.MergeVideos(sessionFolderPaths, sessionId);
    }
    public async Task<string[]> ExtractThumbnails(string originalFolderPath, string thumbnailFolderPath)
    {
        return await AVFoundationHelper.ExtractThumbnails(originalFolderPath, thumbnailFolderPath);
    }
    public async Task<string> ExtractThumbnail(string folderPath, string thumbnailFolderPath)
    {
        return await AVFoundationHelper.ExtractThumbnail(folderPath, thumbnailFolderPath);
    }
    public async Task CompressVideo(string inputPath, string outputPath)
    {
        await AVFoundationHelper.CompressVideoAsync(inputPath, outputPath);
    }
}
