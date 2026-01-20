using FFMpegCore;
using BarClip.Models.Requests;
using System.Text.Json;
using BarClip.Data.Schema;
using static BarClip.Core.Helpers.FileHelper;
using BarClip.Core.Helpers;
using BarClip.Core.Repositories;
using BarClip.Core.Interfaces;

namespace BarClip.Core.Services;

public interface IVideoService
{
    Task<OriginalVideo> CreateOriginalVideo(User user, Session session);
    Task<List<OriginalVideo>> GetOriginalVideosForSession(Guid SessionId);
    //Task<ProcessedVideoRequest> ProcessVideo(SessionFolderPaths sessionFolderPaths, OriginalVideoRequest video);
    Task SaveVideos(SaveVideosRequest request);
    //Task<SaveVideosRequest> TrimVideoFromStorage(string messageText);
    Task UpdateVideos(OriginalVideo original, ProcessedVideo processed);
}

public class VideoService : IVideoService
{
    private readonly StorageService? _storageService;
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
    public async Task SaveVideos(SaveVideosRequest request)
    {
        await _repo.SaveVideosAsync(request);
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
    public async Task<OriginalVideo> CreateOriginalVideo(User user, Session session)
    {
        var video = new OriginalVideo
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            ProcessedVideos = [],
            CurrentProcessedVideoId = Guid.Empty,
            SessionId = session.Id,
            Session = session,
        };
        return await _repo.CreateOriginalVideoAsync(video);
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
