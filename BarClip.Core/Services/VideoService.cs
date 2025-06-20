using BarClip.Data.Schema;
using Microsoft.AspNetCore.Http;
using FFMpegCore;
using BarClip.Core.Repositories;
using BarClip.Models.Requests;
using FFMpegCore.Enums;
using System.Diagnostics;

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

        string compressedPath = Path.Combine(Path.GetTempPath(), $"compressed_{originalVideoFormFile.FileName}");

        var originalVideo = new Video()
        {
            Id = Guid.NewGuid(),
            Name = DateTime.Now.ToString() + Path.GetExtension(originalVideoFormFile.FileName),
            FilePath = compressedPath,
            VideoAnalysis = await FFProbe.AnalyseAsync(tempVideoPath),
            UploadedAt = DateTime.Now,
            TrimmedVideos = new List<TrimmedVideo>()
        };


        var stopwatch = Stopwatch.StartNew();
        await FFMpegArguments
            .FromFileInput(tempVideoPath)
            .OutputToFile(compressedPath, overwrite: true, options => options
                .WithCustomArgument("-vf fps=1 -c:v libx264 -crf 28 -preset veryfast -an"))
            .ProcessAsynchronously();
        stopwatch.Stop();
        Console.WriteLine($"{stopwatch.ElapsedMilliseconds} transcoding");
        originalVideo.Frames = await _frameService.ExtractFrames(originalVideo);

        await _storageService.UploadVideoAsync(originalVideo.Id, originalVideo.FilePath, "originalvideos");

        var trimmedVideo = await _trimService.Trim(originalVideo);

        originalVideo.TrimmedVideos.Add(trimmedVideo);

        originalVideo.CurrentTrimmedVideoId = trimmedVideo.Id;

        await _videoRepository.SaveVideosAsync(originalVideo, trimmedVideo);

        return trimmedVideo;
    }

    public async Task<TrimmedVideo> ReTrimOriginalVideo(ReTrimVideoRequest request)
    {
        Guid trimmedVideoId = request.Id;

        if (request.TrimmedVideoFile is not null)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(request.TrimmedVideoFile.FileName);

            trimmedVideoId = Guid.Parse(fileNameWithoutExtension);
        }

        var originalVideo = await _videoRepository.GetOriginalVideoByTrimmedIdAsync(trimmedVideoId);


        if (originalVideo is null)
        {
            throw new Exception("No original video found.");
        }

        originalVideo.FilePath = await _storageService.DownloadVideoAsync(originalVideo.Id, "originalvideos");

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

        originalVideo.CurrentTrimmedVideoId = trimmedVideo.Id;

        await _videoRepository.SaveVideosAsync(originalVideo, trimmedVideo);

        return trimmedVideo;
    }
}
