using FFMpegCore;
using BarClip.Models.Requests;
using System.Text.Json;
using BarClip.Core.Repositories;
using Microsoft.AspNetCore.Http;
using BarClip.Data.Schema;

namespace BarClip.Core.Services;

public interface IVideoService
{
    Task<SaveVideosRequest> TrimVideoFromStorage(string messageText);
}

public class VideoService : IVideoService
{
    private readonly StorageService _storageService;
    private readonly TrimService _trimService;
    private readonly FrameService _frameService;

    public VideoService(StorageService storageService, TrimService trimService, FrameService frameService)
    {
        _storageService = storageService;
        _trimService = trimService;
        _frameService = frameService;
    }

    public async Task<SaveVideosRequest> TrimVideoFromStorage(string messageText)
    {
        string fileName = GetFileNameFromMessageText(messageText);

        var (videoFilePath, userId) = await _storageService.DownloadVideoAsync(fileName, "originalvideos");

        var originalVideo = new OriginalVideoRequest()
        {
            Id = Guid.NewGuid(),
            FilePath = videoFilePath,
            VideoAnalysis = await FFProbe.AnalyseAsync(videoFilePath),
            UploadedAt = DateTime.Now,
        };

        originalVideo.Frames = await _frameService.ExtractFrames(originalVideo);

        await _storageService.CopyVideoAsync(fileName, originalVideo.Id, "processedvideos");

        var trimmedVideo = await _trimService.Trim(originalVideo);

        originalVideo.CurrentTrimmedVideoId = trimmedVideo.Id;

        var request = new SaveVideosRequest
        {
            OriginalVideo = originalVideo,
            TrimmedVideo = trimmedVideo,
            UserId = userId
        };

        return request;
    }



    //public async Task<string> TrimVideo(IFormFile originalVideoFormFile)
    //{
    //    string tempVideoPath = Path.Combine(Path.GetTempPath(), originalVideoFormFile.FileName);

    //    using (var videoStream = new FileStream(tempVideoPath, FileMode.Create))
    //    {
    //        await originalVideoFormFile.CopyToAsync(videoStream);
    //    }

    //    var originalVideo = new OriginalVideoRequest()
    //    {
    //        Id = Guid.NewGuid(),
    //        Name = DateTime.Now.ToString() + Path.GetExtension(originalVideoFormFile.FileName),
    //        FilePath = tempVideoPath,
    //        VideoAnalysis = await FFProbe.AnalyseAsync(tempVideoPath),
    //        UploadedAt = DateTime.Now,
    //        TrimmedVideos = new List<TrimmedVideoRequest>()
    //    };

    //    originalVideo.Frames = await _frameService.ExtractFrames(originalVideo);

    //    await _storageService.UploadVideoAsync(originalVideo.Id, originalVideo.FilePath, "originalvideos");

    //    var trimmedVideo = await _trimService.Trim(originalVideo);

    //    originalVideo.TrimmedVideos.Add(trimmedVideo);

    //    originalVideo.CurrentTrimmedVideoId = trimmedVideo.Id;

    //    await _videoRepository.SaveVideosAsync(originalVideo, trimmedVideo);

    //    var url = _storageService.GenerateSasUrl(trimmedVideo.Id);

    //    return url;
    //}



    //public async Task<string> ReTrimVideo(ManualTrimRequest request)
    //{
    //    Guid trimmedVideoId = request.Id;

    //    if (request.TrimmedVideoFile is not null)
    //    {
    //        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(request.TrimmedVideoFile.FileName);

    //        trimmedVideoId = Guid.Parse(fileNameWithoutExtension);
    //    }

    //    //var originalVideo = await _videoRepository.GetOriginalVideoByTrimmedIdAsync(trimmedVideoId);


    //    if (originalVideo is null)
    //    {
    //        throw new Exception("No original video found.");
    //    }

    //    originalVideo.FilePath = await _storageService.DownloadVideoAsync(originalVideo.Id.ToString(), "originalvideos");

    //    if (request.StartEarlier)
    //    {
    //        originalVideo.TrimStart = originalVideo.TrimStart - TimeSpan.FromSeconds(request.TrimStart);
    //    }
    //    else
    //    {
    //        originalVideo.TrimStart = originalVideo.TrimStart + TimeSpan.FromSeconds(request.TrimStart);
    //    }

    //    if (request.FinishEarlier)
    //    {
    //        originalVideo.TrimFinish = originalVideo.TrimFinish - TimeSpan.FromSeconds(request.TrimFinish);

    //    }
    //    else
    //    {
    //        originalVideo.TrimFinish = originalVideo.TrimFinish + TimeSpan.FromSeconds(request.TrimFinish);
    //    }

    //    originalVideo.TrimmedVideos = new List<TrimmedVideo>();

    //    var trimmedVideo = await _trimService.Trim(originalVideo);

    //    originalVideo.TrimmedVideos.Add(trimmedVideo);

    //    originalVideo.CurrentTrimmedVideoId = trimmedVideo.Id;

    //    await _videoRepository.SaveVideosAsync(originalVideo, trimmedVideo);

    //    var url = _storageService.GenerateSasUrl(trimmedVideo.Id);

    //    return url;
    //}
    private string GetFileNameFromMessageText(string messageText)
    {
        using var doc = JsonDocument.Parse(messageText);
        var root = doc.RootElement;

        if (!root.TryGetProperty("subject", out JsonElement subjectElement))
            throw new ArgumentException($"Message: {messageText} does not contain 'subject' property.");

        var subject = subjectElement.GetString()
            ?? throw new ArgumentException($"Message: {messageText} does not contain a valid 'subject' property.");

        const string prefix = "/blobServices/default/containers/originalvideos/blobs/";

        if (subject.StartsWith(prefix))
        {
            // Exact case match - normal extraction
            return subject[prefix.Length..];
        }
        else
        {
            throw new ArgumentException($"Unexpected subject format: {subject}");
        }
    }

}
