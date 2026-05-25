using BarClip.Core.Helpers;
using BarClip.Core.Interfaces;
using BarClip.Core.Repositories;
using BarClip.Data.Schema;
using BarClip.Models.Requests;
using BarClip.Models.Responses;
using FFMpegCore;
using System.Text.Json;
using static BarClip.Core.Helpers.FileHelper;

namespace BarClip.Core.Services;

public interface IVideoService
{
    Task<OriginalVideo> CreateOriginalVideo(Guid sessionId, DateTime createdTime);
    Task<List<OriginalVideo>> GetOriginalVideosForSession(Guid SessionId);

    Task UpdateVideos(OriginalVideo original, ProcessedVideo processed);
    Task Upload(UploadRequest request);
    Task<ICollection<Video>> GetAllVideos();
    Task UpsertVideos(IEnumerable<VideoResponse> videos);
    Task UpdateOriginalVideo(OriginalVideo originalVideo);
}

public class VideoService : IVideoService
{
    private readonly StorageService _storageService;
    private readonly TrimService _trimService;
    private readonly FrameService _frameService;
    private readonly PlateAnalysisService _plateAnalysisService;
    private readonly VideoRepository _repo;

    public VideoService(StorageService storageService, TrimService trimService, FrameService frameService, PlateAnalysisService plateAnalysisService, VideoRepository repo)
    {
        _storageService = storageService;
        _trimService = trimService;
        _frameService = frameService;
        _plateAnalysisService = plateAnalysisService;
        _repo = repo;

    }

    public async Task Upload(UploadRequest request)
    {
        if (_storageService != null)
        {
            await _storageService.UploadAsync(request);
        }
    }
    public async Task UpdateOriginalVideo(OriginalVideo originalVideo)
    {
        await _repo.UpdateOriginalVideoAsync(originalVideo);
    }


    public async Task UpdateVideos(OriginalVideo original, ProcessedVideo processed)
    {
        await _repo.AddProcessedVideoAsync(processed);

        original.CurrentProcessedVideoId = processed.Id;

        await _repo.UpdateOriginalVideoAsync(original);
    }
    public async Task<List<OriginalVideo>> GetOriginalVideosForSession(Guid SessionId)
    {
        return await _repo.GetOriginalVideosForSessionAsync(SessionId);
    }
    public async Task<OriginalVideo> CreateOriginalVideo(Guid sessionId, DateTime createdTime)
    {
        var video = new OriginalVideo
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ProcessedVideos = [],
            CurrentProcessedVideoId = Guid.Empty,
            SessionId = sessionId,
            CreatedTime = createdTime,
        };
        return await _repo.CreateOriginalVideoAsync(video);
    }
    public async Task<ICollection<Video>> GetAllVideos()
    {
        return await _repo.GetAllVideosAsync();
    }
    public async Task UpsertVideos(IEnumerable<VideoResponse> videos)
    {
        await _repo.UpsertVideosAsync(videos);
    }

    //public async Task<ProcessedVideoRequest> ProcessVideo(SessionFolderPaths sessionFolderPaths, OriginalVideoRequest video)
    //{
    //    video.Frames = await _frameService.ExtractAndProcessFrames(video);

    //    if (video.TrimStart == TimeSpan.Zero)
    //    {
    //        _plateAnalysisService.SetTrim(video);
    //    }

    //    ProcessedVideoRequest processedVideo = new()
    //    {
    //        Id = Guid.NewGuid(),
    //        FilePath = Path.Combine(sessionFolderPaths.Processed, $"{video.LiftNumber}_Trimmed.MOV"),
    //        Duration = video.TrimFinish - video.TrimStart
    //    };

    //    try
    //    {
    //        string? weightText = null;
    //        if (video.WeightKg is not null)
    //        {
    //            var lbWeight = Math.Floor((decimal)(video.WeightKg * 2.2045));
    //            weightText = $"{video.WeightKg}KG/{lbWeight}LB";
    //        }
    //        await _videoEditor.TrimAndLabelAsync(video, processedVideo, weightText);

    //        return processedVideo;

    //    }
    //    catch (Exception ex)
    //    {
    //        throw new Exception("Error during trimming and labeling: " + ex.Message);
    //    }
    //}
    //public async Task<SaveVideosRequest> TrimVideoFromStorage(string messageText)
    //{
    //    string fileName = GetFileNameFromMessageText(messageText);

    //    var (videoFilePath, entraId) = await _storageService.DownloadVideoAsync(fileName, "originalvideos");

    //    var videoAnalysis = await FFProbe.AnalyseAsync(videoFilePath);

    //    var originalVideo = new OriginalVideoRequest()
    //    {
    //        Id = Guid.NewGuid(),
    //        FilePath = videoFilePath,
    //        VideoAnalysis = videoAnalysis,
    //        UploadedAt = DateTime.Now,
    //    };

    //    originalVideo.Frames = await _frameService.ExtractAndProcessFrames(originalVideo);

    //    await _storageService.CopyVideoAsync(fileName, originalVideo.Id, "processedvideos");

    //    if (originalVideo.TrimStart == TimeSpan.Zero)
    //    {
    //        _plateAnalysisService.SetTrim(originalVideo);
    //    }
    //    var trimmedVideo = await _trimService.Trim(originalVideo);

    //    originalVideo.CurrentTrimmedVideoId = trimmedVideo.Id;

    //    var request = new SaveVideosRequest
    //    {
    //        OriginalVideo = originalVideo,
    //        TrimmedVideo = trimmedVideo,
    //        EntraId = entraId
    //    };

    //    return request;
    //}
    //private string GetFileNameFromMessageText(string messageText)
    //{
    //    using var doc = JsonDocument.Parse(messageText);
    //    var root = doc.RootElement;

    //    if (!root.TryGetProperty("subject", out JsonElement subjectElement))
    //        throw new ArgumentException($"Message: {messageText} does not contain 'subject' property.");

    //    var subject = subjectElement.GetString()
    //        ?? throw new ArgumentException($"Message: {messageText} does not contain a valid 'subject' property.");

    //    const string prefix = "/blobServices/default/containers/originalvideos/blobs/";

    //    if (subject.StartsWith(prefix))
    //    {
    //        // Exact case match - normal extraction
    //        return subject[prefix.Length..];
    //    }
    //    else
    //    {
    //        throw new ArgumentException($"Unexpected subject format: {subject}");
    //    }
    //}

}
