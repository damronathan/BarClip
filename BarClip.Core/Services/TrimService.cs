using BarClip.Data.Schema;
using FFMpegCore;
using BarClip.Models.Domain;
namespace BarClip.Core.Services;

public class TrimService
{
    private readonly StorageService _storageService;

    public TrimService(StorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<TrimmedVideo> Trim(Video video)
        {
        if (video.TrimStart == TimeSpan.Zero)
        {
            SetTrim(video);
        }

        string tempTrimmedVideoPath = Path.GetTempPath();

        TrimmedVideo trimmedVideo = new()
        {
            Id = Guid.NewGuid(),
            Name = $"{DateTime.Now}",
            FilePath = Path.Combine(tempTrimmedVideoPath, "trimmed-video.mp4"),
            OriginalVideo = video,
            OriginalVideoId = video.Id,
            Duration = video.TrimFinish - video.TrimStart
        };

        try
        {
            await FFMpegArguments
                .FromFileInput(video.FilePath)
                .OutputToFile(trimmedVideo.FilePath, overwrite: true, options => options
                .Seek(video.TrimStart)
                .WithDuration(trimmedVideo.Duration)
                .WithCustomArgument("-c copy"))
                .ProcessAsynchronously();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error trimming video: {ex.Message}");
        }

        await _storageService.UploadVideoAsync(trimmedVideo.Id, trimmedVideo.FilePath, "trimmedvideos");

        return trimmedVideo;
    }

    private Video SetTrim(Video video)
    {
        int frameNumber = GetStartFrame(video);
        video.TrimStart = TimeSpan.FromSeconds(frameNumber);
        video.TrimFinish = GetTrimFinish(frameNumber, video);
        return video;
    }

    private static int GetStartFrame(Video video)
    {
        float yValue = 0f;
        bool initialYFound = false;
        int frameNumber = 0;
        PlateDetection previousDetection = null;

        foreach (Frame frame in video.Frames)
        {
            if (frame.PlateDetections.Count > 0)
            {
                PlateDetection plateDetection = SelectBestDetection(frame, previousDetection);

                if (!initialYFound)
                {
                    yValue = plateDetection.Y;
                    initialYFound = true;
                }

                if (initialYFound && Math.Abs(plateDetection.Y - yValue) > 50f)
                {
                    frameNumber = frame.FrameNumber;
                    break;
                }

                previousDetection = plateDetection;
            }
        }

        return Math.Max(frameNumber - 3, 0);
    }

    private static TimeSpan GetTrimFinish(int trim, Video video)
    {
        PlateDetection? previousDetection = null;

        for (int i = video.Frames.Count - 1; i >= trim; i--)
        {
            Frame frame = video.Frames[i];

            if (frame.PlateDetections.Count > 0)
            {
                PlateDetection currentDetection = SelectBestDetection(frame, previousDetection);
                if (currentDetection == null)
                {
                    continue;
                }

                if (previousDetection != null)
                {
                    if (Math.Abs(currentDetection.Y - previousDetection.Y) > 50f)
                    {
                        return TimeSpan.FromSeconds(frame.FrameNumber + 3);
                    }
                }

                previousDetection = currentDetection;
            }
            continue;
        }
        return TimeSpan.FromSeconds(video.Frames.Last().FrameNumber - 2);
    }

    private static PlateDetection SelectBestDetection(Frame frame, PlateDetection referenceDetection)
    {
        if (frame.PlateDetections is null)
            return referenceDetection;

        if (referenceDetection == null)
            return frame.PlateDetections.OrderByDescending(pd => pd.Confidence).First();

        var candidateDetections = frame.PlateDetections
        .Where(pd => Math.Abs(pd.X - referenceDetection.X) < 50)
        .ToList();

        if (candidateDetections.Any())
        {
            return candidateDetections.OrderBy(pd => Math.Abs(pd.X - referenceDetection.X)).First();
        }

        if (frame.FrameNumber > 5)
        {
            var closestByX = frame.PlateDetections.OrderBy(pd => Math.Abs(pd.X - referenceDetection.X)).First();

            if (Math.Abs(closestByX.Height - referenceDetection.Height) < 20)
            {
                return closestByX;
            }
            else
            {
                return referenceDetection;
            }
        }



        return referenceDetection;
    }
}