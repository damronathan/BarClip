using BarClip.Data.Schema;
using Microsoft.AspNetCore.Http;
using FFMpegCore;
using BarClip.Core.Repositories;
using BarClip.Models.Requests;

namespace BarClip.Core.Services;

public class VideoService
{
    private readonly StorageService _storageService;
    private readonly TrimService _trimService;
    private readonly VideoRepository _videoRepository;
    private readonly FrameService _frameService;

    public VideoService(StorageService storageService, TrimService trimService, VideoRepository videoRepository, FrameService frameService)
    {
        _storageService = storageService;
        _trimService = trimService;
        _videoRepository = videoRepository;
        _frameService = frameService;
    }

    public async Task<TrimmedVideo> TrimOriginalVideo(IFormFile originalVideoFormFile)
    {
        string tempVideoPath = Path.Combine(Path.GetTempPath(), originalVideoFormFile.FileName);

        using (var videoStream = new FileStream(tempVideoPath, FileMode.Create))
        {
            await originalVideoFormFile.CopyToAsync(videoStream);
        }

        var originalVideo = new Video()
        {
            Id = Guid.NewGuid(),
            Name = DateTime.Now.ToString() + Path.GetExtension(originalVideoFormFile.FileName),
            FilePath = tempVideoPath,
            VideoAnalysis = await FFProbe.AnalyseAsync(tempVideoPath),
            UploadedAt = DateTime.Now,
            TrimmedVideos = new List<TrimmedVideo>()
        };

        originalVideo.Frames = await _frameService.ExtractFrames(originalVideo);

        await _storageService.UploadVideoAsync(originalVideo.Id, originalVideo.FilePath, "originalvideos");

        var trimmedVideo = await _trimService.Trim(originalVideo);

        originalVideo.TrimmedVideos.Add(trimmedVideo);

        await _videoRepository.SaveVideosAsync(originalVideo, trimmedVideo);

        return trimmedVideo;
    }

    public async Task<TrimmedVideo> ReTrimOriginalVideo(ReTrimVideoRequest request)
    {
        var originalVideo = await _videoRepository.GetOriginalVideoByIdAsync(request.Id);

        if (originalVideo is null)
        {
            throw new Exception("No original video found.");
        }

        originalVideo.FilePath = await _storageService.DownloadVideoAsync(originalVideo.Id);

        if (request.StartEarlier)
        {
            originalVideo.TrimStart = originalVideo.TrimStart - TimeSpan.FromSeconds(request.TrimStart);
        }
        else
        {
            originalVideo.TrimStart = originalVideo.TrimStart + TimeSpan.FromSeconds(request.TrimStart);
        }

        if (request.FinishEarlier)
        {
            originalVideo.TrimFinish = originalVideo.TrimFinish - TimeSpan.FromSeconds(request.TrimFinish);

        }
        else
        {
            originalVideo.TrimFinish = originalVideo.TrimFinish + TimeSpan.FromSeconds(request.TrimFinish);
        }

        originalVideo.TrimmedVideos = new List<TrimmedVideo>();

        var trimmedVideo = await _trimService.Trim(originalVideo);

        originalVideo.TrimmedVideos.Add(trimmedVideo);

        await _videoRepository.SaveVideosAsync(originalVideo, trimmedVideo);

        return trimmedVideo;
    }
}
