using BarClip.Models.Domain;
using BarClip.Models.Requests;

namespace BarClip.Core.Services;

public class PlateIdentity
{
    public int Id { get; set; }
    public float BaselineY { get; set; }
    public float BaselineHeight { get; set; }
    public float CurrentY { get; set; }
    public float CurrentX { get; set; }
    public float CurrentHeight { get; set; }
    public int LastSeenFrame { get; set; }
    public bool HasMoved { get; set; }
}

public class PlateAnalysisService
{
    private const float HeightMatchThresholdPercent = 0.15f; // 20% of baseline height
    private const int NoDetectionFrameLimit = 5;

    public void Log(string message) =>
        System.Diagnostics.Debug.WriteLine($"[PlateAnalysis] {message}");

    public void SetTrim(OriginalVideoRequest video)
    {
        var (trimStart, trimFinish) = AnalyzeVideo(video);
        video.TrimStart = trimStart;
        video.TrimFinish = trimFinish;
    }

    public (TimeSpan TrimStart, TimeSpan TrimFinish) AnalyzeVideo(OriginalVideoRequest video)
    {
        var plates = new List<PlateIdentity>();
        int? trimStartFrame = null;
        int lastFrameWithDetection = -1;

        Log($"Analyzing video with {video.Frames.Count} frames.");

        // Single forward pass
        foreach (var frame in video.Frames)
        {
            if (frame.PlateDetections == null || frame.PlateDetections.Count == 0)
                continue;

            lastFrameWithDetection = frame.FrameNumber;
            Log($"Frame {frame.FrameNumber}: {frame.PlateDetections.Count} detection(s)");

            foreach (var detection in frame.PlateDetections)
            {
                var matchedPlate = FindMatchingPlate(plates, detection);

                if (matchedPlate == null)
                {
                    // New plate identity
                    var newPlate = new PlateIdentity
                    {
                        Id = plates.Count + 1,
                        BaselineY = detection.Y,
                        BaselineHeight = detection.Height,
                        CurrentY = detection.Y,
                        CurrentX = detection.X,
                        CurrentHeight = detection.Height,
                        LastSeenFrame = frame.FrameNumber,
                        HasMoved = false
                    };
                    plates.Add(newPlate);
                    Log($"New plate identity {newPlate.Id} initialized at Y:{detection.Y:F1} Height:{detection.Height:F1}");
                }
                else
                {
                    float yDelta = Math.Abs(detection.Y - matchedPlate.BaselineY);
                    float movementThreshold = matchedPlate.BaselineHeight / 2f;

                    Log($"Plate {matchedPlate.Id} - Y:{detection.Y:F1} Delta:{yDelta:F1} Threshold:{movementThreshold:F1}");

                    if (yDelta > movementThreshold && !matchedPlate.HasMoved)
                    {
                        matchedPlate.HasMoved = true;
                        Log($"Plate {matchedPlate.Id} movement detected at frame {frame.FrameNumber}");

                        if (trimStartFrame == null)
                        {
                            trimStartFrame = frame.FrameNumber;
                            Log($"Trim start set at frame {trimStartFrame}");
                        }
                    }

                    matchedPlate.CurrentY = detection.Y;
                    matchedPlate.CurrentX = detection.X;
                    matchedPlate.CurrentHeight = detection.Height;
                    matchedPlate.LastSeenFrame = frame.FrameNumber;
                }
            }
        }

        // Check if last 5 frames had no detections
        int lastFrame = video.Frames.Count - 1;
        bool missingEndDetections = (lastFrame - lastFrameWithDetection) >= NoDetectionFrameLimit;

        if (trimStartFrame == null)
        {
            Log("No movement detected. Returning full video.");
            return (TimeSpan.Zero, video.Duration);
        }

        if (missingEndDetections)
        {
            Log($"No detections in last {NoDetectionFrameLimit} frames. Returning full video duration for trim end.");
            return (TimeSpan.FromSeconds(Math.Max(trimStartFrame.Value - 1, 0)), video.Duration);
        }

        // Derive trim end by scanning frame history backward
        TimeSpan trimFinish = GetTrimFinish(video, plates, trimStartFrame.Value);

        TimeSpan trimStart = TimeSpan.FromSeconds(Math.Max(trimStartFrame.Value - 1, 0));
        Log($"Final trim - Start: {trimStart} Finish: {trimFinish}");

        return (trimStart, trimFinish);
    }

    private TimeSpan GetTrimFinish(OriginalVideoRequest video, List<PlateIdentity> plates, int trimStartFrame)
    {
        // Establish resting Y from last frame with detections
        var lastFrameWithDetections = video.Frames
            .LastOrDefault(f => f.PlateDetections != null && f.PlateDetections.Count > 0);

        if (lastFrameWithDetections == null)
        {
            Log("No detections found for resting position. Returning full duration.");
            return video.Duration;
        }

        // Match last frame detections to plate identities to set resting Y
        var restingY = new Dictionary<int, float>();
        foreach (var detection in lastFrameWithDetections.PlateDetections)
        {
            var matchedPlate = FindMatchingPlate(plates, detection);
            if (matchedPlate != null && !restingY.ContainsKey(matchedPlate.Id))
            {
                restingY[matchedPlate.Id] = detection.Y;
                Log($"Plate {matchedPlate.Id} resting Y set to {detection.Y:F1} from frame {lastFrameWithDetections.FrameNumber}");
            }
        }

        // Scan backward to find last frame where any plate deviated from resting Y
        int lastMovementFrame = trimStartFrame;

        for (int i = video.Frames.Count - 1; i >= trimStartFrame; i--)
        {
            var frame = video.Frames[i];

            if (frame.PlateDetections == null || frame.PlateDetections.Count == 0)
                continue;

            bool movementFound = false;

            foreach (var detection in frame.PlateDetections)
            {
                var matchedPlate = FindMatchingPlate(plates, detection);
                if (matchedPlate == null || !restingY.ContainsKey(matchedPlate.Id))
                    continue;

                float yDelta = Math.Abs(detection.Y - restingY[matchedPlate.Id]);
                float movementThreshold = matchedPlate.BaselineHeight / 2f;

                Log($"Frame {frame.FrameNumber} - Plate {matchedPlate.Id} Y:{detection.Y:F1} RestingY:{restingY[matchedPlate.Id]:F1} Delta:{yDelta:F1} Threshold:{movementThreshold:F1}");

                if (yDelta > movementThreshold)
                {
                    lastMovementFrame = frame.FrameNumber;
                    movementFound = true;
                    Log($"Plate {matchedPlate.Id} still displaced at frame {frame.FrameNumber}");
                    break;
                }
            }

            if (movementFound)
                break;
        }

        double endFrame = lastMovementFrame + 1.5;
        Log($"Trim end found at frame {lastMovementFrame}, setting finish to {endFrame}");
        return TimeSpan.FromSeconds(endFrame);
    }
    private PlateIdentity? FindMatchingPlate(List<PlateIdentity> plates, PlateDetection detection)
    {
        if (!plates.Any()) return null;

        PlateIdentity? bestMatch = null;
        float bestHeightDelta = float.MaxValue;

        foreach (var plate in plates)
        {
            float heightDelta = Math.Abs(detection.Height - plate.BaselineHeight);
            float threshold = plate.BaselineHeight * HeightMatchThresholdPercent;

            if (heightDelta <= threshold && heightDelta < bestHeightDelta)
            {
                bestHeightDelta = heightDelta;
                bestMatch = plate;
            }
        }

        return bestMatch;
    }
}