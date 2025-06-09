using Xabe.FFmpeg;
using BarClip.Data.Schema;
using Microsoft.AspNetCore.Http;

namespace BarClip.Core.Services;

public class VideoService
{
    public static async Task TrimOriginalVideo(IFormFile originalVideoFormFile)
    {
        string tempVideoPath = Path.Combine(Path.GetTempPath() + originalVideoFormFile.FileName);

        using (var videoStream = new FileStream(tempVideoPath, FileMode.Create))
        {
            await originalVideoFormFile.CopyToAsync(videoStream);
        }

        var originalVideo = new Video()
        {
            Id = Guid.NewGuid(),
            Name = originalVideoFormFile.FileName,
            FilePath = tempVideoPath,
            VideoInfo = await FFmpeg.GetMediaInfo(tempVideoPath)
        };

        await StorageService.UploadVideo(originalVideo.Id, originalVideo.FilePath, "originalvideos");

        originalVideo.Frames = await FrameService.ExtractFrames(originalVideo);

        await TrimService.Trim(originalVideo);
    }

}
