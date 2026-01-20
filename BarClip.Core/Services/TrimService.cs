using BarClip.Models.Domain;
using BarClip.Models.Requests;
using BarClip.Core.Helpers;
namespace BarClip.Core.Services;

public class TrimService
{
    private readonly StorageService _storageService;

    public TrimService(StorageService storageService)
    {
        _storageService = storageService;

        var ffmpegFullPath = Path.Combine(AppContext.BaseDirectory);
    }

    //public async Task<ProcessedVideoRequest> Trim(OriginalVideoRequest video)
    //    {

    //    string tempTrimmedVideoPath = Path.GetTempPath();
    //    var id = Guid.NewGuid();

    //    ProcessedVideoRequest trimmedVideo = new()
    //    {
    //        Id = id,
    //        FilePath = Path.Combine(tempTrimmedVideoPath, $"{id}.mp4"),
    //        Duration = video.TrimFinish - video.TrimStart
    //    };

    //    try
    //    {
    //        await FFMpegHelper.TrimAsync(video, trimmedVideo);


    //        await _storageService.UploadVideoAsync(trimmedVideo.Id, trimmedVideo.FilePath, "trimmedvideos");
    //    }
    //    catch (Exception ex)
    //    {
    //        throw new Exception($"Error trimming video: {ex.Message}");
    //    }
    //    finally
    //    {
    //        File.Delete(video.FilePath);
    //        File.Delete(trimmedVideo.FilePath);
    //    }


    //    return trimmedVideo;
    //}

    
}