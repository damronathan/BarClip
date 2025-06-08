using BarClip.Data.Schema;
using Xabe.FFmpeg;

namespace BarClip.Core.Services;

public class TrimService
{
    public static async Task<TrimmedVideo> Trim(Video video)
    {
        (TimeSpan startTime, TimeSpan finishTime) = TrimService.GetTrim(video);

        IVideoStream videoStream = video.VideoInfo.VideoStreams.First();

        IVideoStream trimmedVideoStream = videoStream.Split(startTime, finishTime - startTime);

        string outputPath = @"C:\ImageDetectorVideos\TrimmedVideos";

        Directory.CreateDirectory(outputPath);

        TrimmedVideo trimmedVideo = new()
        {
            Name = "trimmed_" + video.Name,
            FilePath = Path.Combine(outputPath, "trimmed-" + video.Name),
            OriginalVideo = video
        };

        try
        {
            var conversion = FFmpeg.Conversions.New()
                .AddStream(trimmedVideoStream)
                .SetOutput(trimmedVideo.FilePath);

            var result = await conversion.Start();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error trimming video: {ex.Message}");
        }
        return trimmedVideo;
    }
    private static (TimeSpan, TimeSpan) GetTrim(Video video)
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

        return Math.Max(frameNumber - 2, 0);
    }

    private static TimeSpan GetTrimFinish(int trim, Video video)
    {
        float yValue = 0f;
        int consecutiveStableFrames = 0;
        PlateDetection plateDetection = new();
        PlateDetection previousDetection = null;

        for (int i = trim; i < video.Frames.Count; i++)
        {
            Frame frame = video.Frames[i];
            foreach (PlateDetection detection in frame.PlateDetections)
            {
                if (i == trim && detection.Confidence > plateDetection.Confidence)
                {
                    plateDetection = detection;
                }

                switch (frame.PlateDetections.Count)
                {
                    case 0:
                        break;
                    case 1:
                        plateDetection = detection;
                        break;
                    default:
                        float targetX = plateDetection.X;
                        PlateDetection closestDetection = frame.PlateDetections
                            .OrderBy(pd => Math.Abs(pd.X - targetX))
                            .First();
                        plateDetection = closestDetection;
                        break;
                }
            }

            if (i == trim)
            {
                yValue = plateDetection.Y;
            }
            else
            {
                if (Math.Abs(plateDetection.Y - yValue) < 10f)
                {
                    consecutiveStableFrames++;
                    if (consecutiveStableFrames >= 3)
                    {
                        int finishFrame = frame.FrameNumber - 2;
                        return TimeSpan.FromSeconds(Math.Max(finishFrame, 0));
                    }
                }
                else
                {
                    consecutiveStableFrames = 0;
                }

                yValue = plateDetection.Y;
            }
        }

        return TimeSpan.FromSeconds(video.Frames.Last().FrameNumber);
    }

    private static PlateDetection SelectBestDetection(Frame frame, PlateDetection referenceDetection)
    {
        if (frame.PlateDetections.Count == 0)
            return new PlateDetection();

        if (frame.PlateDetections.Count == 1)
            return frame.PlateDetections[0];

        float targetX = referenceDetection?.X ?? frame.PlateDetections[0].X;
        return frame.PlateDetections
            .OrderBy(pd => Math.Abs(pd.X - targetX))
            .First();
    }

}
