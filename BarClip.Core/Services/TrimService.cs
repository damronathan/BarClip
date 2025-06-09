using BarClip.Data.Schema;
using FFMpegCore;
using FFMpegCore.Enums;

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
        (TimeSpan startTime, TimeSpan finishTime) = GetTrim(video);

        string tempTrimmedVideoPath = Path.GetTempPath();

        TrimmedVideo trimmedVideo = new()
        {
            Id = Guid.NewGuid(),
            OriginalVideoId = video.Id,
            OriginalVideo = video,
            FilePath = Path.Combine(tempTrimmedVideoPath, "trimmed-video.mp4"),
            Duration = finishTime - startTime
        };

        try
        {
            await FFMpegArguments
                .FromFileInput(video.FilePath)

                .OutputToFile(trimmedVideo.FilePath, overwrite: true, options => options
                .Seek(startTime)
                .WithDuration(trimmedVideo.Duration)
                .WithCustomArgument("-c copy"))
                .ProcessAsynchronously();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error trimming video: {ex.Message}");
        }

        await _storageService.UploadVideo(trimmedVideo.Id, trimmedVideo.FilePath, "trimmedvideos"); // fast

        return trimmedVideo;
    }

    private (TimeSpan, TimeSpan) GetTrim(Video video)
    {
        int frameNumber = GetStartFrame(video);
        TimeSpan trimStart = TimeSpan.FromSeconds(frameNumber);
        TimeSpan trimFinish = GetTrimFinish(frameNumber, video);
        return (trimStart, trimFinish);
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