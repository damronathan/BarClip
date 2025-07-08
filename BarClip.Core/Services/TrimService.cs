using BarClip.Data.Schema;
using FFMpegCore;
using BarClip.Models.Domain;
using BarClip.Models.Options;
using Microsoft.Extensions.Options;
using BarClip.Models.Requests;
namespace BarClip.Core.Services;

public class TrimService
{
    private readonly StorageService _storageService;

    public TrimService(StorageService storageService)
    {
        _storageService = storageService;

        var ffmpegFullPath = Path.Combine(AppContext.BaseDirectory);
    }

    public async Task<TrimmedVideoRequest> Trim(OriginalVideoRequest video)
        {
        if (video.TrimStart == TimeSpan.Zero)
        {
            SetTrim(video);
        }

        string tempTrimmedVideoPath = Path.GetTempPath();
        var id = Guid.NewGuid();

        TrimmedVideoRequest trimmedVideo = new()
        {
            Id = id,
            FilePath = Path.Combine(tempTrimmedVideoPath, $"{id}.mp4"),
            OriginalVideo = video,
            OriginalVideoId = video.Id,
            Duration = video.TrimFinish - video.TrimStart
        };

        try
        {
            await FFMpegArguments
                .FromFileInput(video.FilePath, true, options => options
                .Seek(video.TrimStart))
                .OutputToFile(trimmedVideo.FilePath, overwrite: true, options => options
                .WithDuration(trimmedVideo.Duration)
                .WithCustomArgument("-c:v copy -c:a aac"))
                .ProcessAsynchronously();

            await _storageService.UploadVideoAsync(trimmedVideo.Id, trimmedVideo.FilePath, "trimmedvideos");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error trimming video: {ex.Message}");
        }
        finally
        {
            File.Delete(video.FilePath);
            File.Delete(trimmedVideo.FilePath);
        }


        return trimmedVideo;
    }

    private void SetTrim(OriginalVideoRequest video)
    {
        int frameNumber = GetStartFrame(video);
        video.TrimStart = TimeSpan.FromSeconds(frameNumber);
        video.TrimFinish = GetTrimFinish(frameNumber, video);
    }

    private static int GetStartFrame(OriginalVideoRequest video)
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

    private static TimeSpan GetTrimFinish(int trim, OriginalVideoRequest video)
    {
        PlateDetection? previousDetection = null;

        bool lastDetectionIsCurrent = false;

        //Loop starting from the end of the video
        for (int i = video.Frames.Count - 1; i >= trim; i--)
        { 
            Frame frame = video.Frames[i];

            if (frame.PlateDetections.Count > 0)
            {
                (PlateDetection currentDetection, lastDetectionIsCurrent) = SelectBestDetection(frame, previousDetection, lastDetectionIsCurrent);

                //Only null if no detection found yet
                if (currentDetection is null)
                {
                    continue;
                }

                if (previousDetection is not null)
                {
                    //If plate has moved up or down
                    if (Math.Abs(currentDetection.Y - previousDetection.Y) > 50f)
                    {
                        if (lastDetectionIsCurrent is false)
                        {
                            //If this is past the second detection, create the trim point
                            return TimeSpan.FromSeconds(frame.FrameNumber + 3);
                        }
                        else
                        {
                            //If first 2 detections have vertical movement, return whole video.
                            return TimeSpan.FromSeconds(video.Frames.Last().FrameNumber);
                        }

                    }

                }

                previousDetection = currentDetection;
            }
        }
        return TimeSpan.FromSeconds(video.Frames.Last().FrameNumber - 2);
    }

    private static PlateDetection SelectBestDetection(Frame frame, PlateDetection referenceDetection)
    {
        var (detection, _) = SelectBestDetection(frame, referenceDetection, false);
        return detection;
    }
    private static (PlateDetection, bool) SelectBestDetection(Frame frame, PlateDetection referenceDetection, bool lastDetectionIsCurrent)
    {
        //If no detections in frame, return null or reference detection
        if (frame.PlateDetections is null)
            return (referenceDetection, lastDetectionIsCurrent);

        //If it is the first frame, or no detections yet, reference detection will be null
        //Return the best detection from the frame, this is the last detection
        if (referenceDetection is null)
        {
            var lastDetection = frame.PlateDetections.OrderByDescending(pd => pd.Confidence).First();
            return (lastDetection, true);
        }

        //Filters out the second plate
        var candidateDetections = frame.PlateDetections
        .Where(pd => Math.Abs(pd.X - referenceDetection.X) < 50)
        .ToList();

        //Selects the current plate. Checks to make sure the second isn't close enough to make the list
        if (candidateDetections.Any())
        {
            var currentDetection = candidateDetections.OrderBy(pd => Math.Abs(pd.X - referenceDetection.X)).First();
            return (currentDetection, false);
        }

        //Allows second plate to be selected if close enough y value to first plate after 5th frame
        //The y check is to prevent false movement triggers
        //sometimes first plate will be obstructed and second plate is valid to check height change.
        if (frame.FrameNumber > 5)
        {
            var closestByX = frame.PlateDetections.OrderBy(pd => Math.Abs(pd.X - referenceDetection.X)).First();

            if (Math.Abs(closestByX.Height - referenceDetection.Height) < 20)
            {
                return (closestByX, false);
            }
            else
            {
                return (referenceDetection, false);
            }
        }
        return (referenceDetection, false);
    }
}