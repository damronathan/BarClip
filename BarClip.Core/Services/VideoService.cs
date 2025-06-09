using BarClip.Data.Schema;
using Microsoft.AspNetCore.Http;
using FFMpegCore;

namespace BarClip.Core.Services;

public class VideoService
{
    private readonly StorageService _storageService;
    private readonly TrimService _trimService;

    public VideoService(StorageService storageService, TrimService trimService)
    {
        _storageService = storageService;
        _trimService = trimService;
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
            Name = originalVideoFormFile.FileName,
            FilePath = tempVideoPath,
            VideoAnalysis = await FFProbe.AnalyseAsync(tempVideoPath)
        };

        originalVideo.Frames = await FrameService.ExtractFrames(originalVideo);

        return await _trimService.Trim(originalVideo);
    }
}
