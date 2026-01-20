using BarClip.Models.Domain;
using BarClip.Models.Requests;
using Microsoft.Identity.Client;

namespace BarClip.Core.Services;
public class PlateAnalysisService
{
    public void SetTrim(OriginalVideoRequest video)
    {
        int frameNumber = GetStartFrame(video);
        video.TrimStart = TimeSpan.FromSeconds(frameNumber - .5);
        video.TrimFinish = GetTrimFinish(frameNumber, video);
    }

    public void Log(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[PlateAnalysis] {message}");
    }

    public int GetStartFrame(OriginalVideoRequest video)
    {
        float yValue = 0f;
        bool initialYFound = false;
        int frameNumber = 0;
        PlateDetection previousDetection = null;
        Log($"Setting trim for video with {video.Frames.Count} frames.");

        foreach (Frame frame in video.Frames)
        {
            if (frame.PlateDetections.Count > 0)
            {
                Log($"{frame.PlateDetections.Count} detections found for frame {frame.FrameNumber}");
                int x = 1;
                foreach (var detection in frame.PlateDetections)
                {
                    Log($"Detection {x}:");
                    Log($"Confidence: {detection.Confidence}");
                    Log($"Y: {detection.Y}");
                    Log($"X: {detection.X}");
                    Log($"Width: {detection.Width}");
                    Log($"Height: {detection.Height}");
                    x++;
                }
                PlateDetection plateDetection = SelectBestDetection(frame, previousDetection);
                Log($"Selected Detection for frame {frame.FrameNumber}:");
                Log($"Confidence: {plateDetection.Confidence}");
                Log($"Y: {plateDetection.Y}");
                Log($"X: {plateDetection.X}");
                Log($"Width: {plateDetection.Width}");
                Log($"Height: {plateDetection.Height}");

                if (!initialYFound)
                {
                    yValue = plateDetection.Y;
                    initialYFound = true;
                    Log($"Initial Y: {yValue}");
                }

                if (initialYFound && Math.Abs(plateDetection.Y - yValue) > plateDetection.Height / 2)
                {
                    frameNumber = frame.FrameNumber;
                    Log($"Start frame: {frameNumber}. Detection moved from {yValue} to {plateDetection.Y}");
                    Log($"Trim Start found at {frameNumber - 1}.");
                    break;
                }

                previousDetection = plateDetection;
            }
        }

        return Math.Max(frameNumber - 1, 1);
    }

    public TimeSpan GetTrimFinish(int trim, OriginalVideoRequest video)
    {
        PlateDetection? previousDetection = null;
        bool lastDetectionIsCurrent = false;

        Log($"Starting backward scan from frame {video.Frames.Count - 1} to frame {trim}");

        //Loop starting from the end of the video
        for (int i = video.Frames.Count - 1; i >= trim; i--)
        {
            Frame frame = video.Frames[i];

            if (frame.PlateDetections.Count > 0)
            {
                Log($"{frame.PlateDetections.Count} detections found for frame {frame.FrameNumber} (backward scan)");

                (PlateDetection currentDetection, lastDetectionIsCurrent) = SelectBestDetection(frame, previousDetection, lastDetectionIsCurrent);

                //Only null if no detection found yet
                if (currentDetection is null)
                {
                    Log($"No valid detection selected for frame {frame.FrameNumber}");
                    continue;
                }

                Log($"Selected Detection for frame {frame.FrameNumber}:");
                Log($"Y: {currentDetection.Y}");
                Log($"X: {currentDetection.X}");

                if (previousDetection is not null)
                {
                    Log($"Comparing Y movement: Current {currentDetection.Y} vs Previous {previousDetection.Y}, Delta: {Math.Abs(currentDetection.Y - previousDetection.Y)}");

                    //If plate has moved up or down
                    if (Math.Abs(currentDetection.Y - previousDetection.Y) > currentDetection.Height / 2)
                    {
                        if (lastDetectionIsCurrent is false)
                        {
                            double endFrame = frame.FrameNumber + 1.5;
                            Log($"Trim End found at frame {frame.FrameNumber}. Movement detected from {previousDetection.Y} to {currentDetection.Y}. Setting end to {endFrame}");
                            return TimeSpan.FromSeconds(endFrame);
                            //If this is past the second detection, create the trim point
                        }
                        else
                        {
                            Log($"Movement detected too early in backward scan (first 2 detections). Returning full video duration.");
                            //If first 2 detections have vertical movement, return whole video.
                            return video.VideoAnalysis.Duration;
                        }
                    }
                }

                previousDetection = currentDetection;
            }
        }

        Log($"No end movement detected in backward scan. Returning full video duration.");
        return video.VideoAnalysis.Duration;
    }

    public PlateDetection SelectBestDetection(Frame frame, PlateDetection referenceDetection)
    {
        var (detection, _) = SelectBestDetection(frame, referenceDetection, false);
        return detection;
    }
    public static (PlateDetection, bool) SelectBestDetection(Frame frame, PlateDetection referenceDetection, bool lastDetectionIsCurrent)
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
