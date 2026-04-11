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
    private const float HeightMatchThresholdPercent = 0.20f;
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
        const int LockInFrame = 15;

        Log($"Analyzing video with {video.Frames.Count} frames.");

        foreach (var frame in video.Frames)
        {
            if (frame.PlateDetections == null || frame.PlateDetections.Count == 0)
                continue;

            lastFrameWithDetection = frame.FrameNumber;
            Log($"Frame {frame.FrameNumber}: {frame.PlateDetections.Count} detection(s)");

            if (frame.FrameNumber <= LockInFrame)
            {
                // Identity establishment phase - match by height or create new
                foreach (var detection in frame.PlateDetections.OrderByDescending(d => d.Height))
                {
                    var matchedPlate = FindMatchingPlate(plates, detection);

                    if (matchedPlate == null)
                    {
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

                        // Re-rank plates by baseline height descending
                        plates = plates.OrderByDescending(p => p.BaselineHeight).ToList();
                        for (int i = 0; i < plates.Count; i++)
                            plates[i].Id = i + 1;

                        Log($"New plate identity {newPlate.Id} initialized at Y:{detection.Y:F1} Height:{detection.Height:F1}");
                    }
                    else
                    {
                        UpdatePlate(matchedPlate, detection, frame.FrameNumber);
                        CheckMovement(matchedPlate, detection, frame.FrameNumber, ref trimStartFrame);
                    }
                }
            }
            else
            {
                // Ranking phase - assign detections to identities by closest height
                var sortedDetections = frame.PlateDetections.OrderByDescending(d => d.Height).ToList();
                var assignedPlates = new HashSet<int>();

                foreach (var detection in sortedDetections)
                {
                    var matchedPlate = plates
                        .Where(p => !assignedPlates.Contains(p.Id))
                        .OrderBy(p => Math.Abs(detection.Height - p.CurrentHeight))
                        .FirstOrDefault();

                    if (matchedPlate == null) continue;

                    assignedPlates.Add(matchedPlate.Id);
                    UpdatePlate(matchedPlate, detection, frame.FrameNumber);
                    CheckMovement(matchedPlate, detection, frame.FrameNumber, ref trimStartFrame);
                }
            }
        }

        int lastFrame = video.Frames.Last().FrameNumber;
        bool missingEndDetections = (lastFrame - lastFrameWithDetection) >= NoDetectionFrameLimit;

        if (trimStartFrame == null)
        {
            Log("No movement detected. Returning full video.");
            return (TimeSpan.Zero, video.Duration);
        }

        if (missingEndDetections)
        {
            Log($"No detections in last {NoDetectionFrameLimit} frames. Returning full duration for trim end.");
            return (TimeSpan.FromSeconds(Math.Max(trimStartFrame.Value - 1, 0)), video.Duration);
        }

        TimeSpan trimFinish = GetTrimFinish(video, plates, trimStartFrame.Value);
        TimeSpan trimStart = TimeSpan.FromSeconds(Math.Max(trimStartFrame.Value - 2, 0));
        Log($"Final trim - Start: {trimStart} Finish: {trimFinish}");

        return (trimStart, trimFinish);
    }
    private TimeSpan GetTrimFinish(OriginalVideoRequest video, List<PlateIdentity> plates, int trimStartFrame)
{
    // Establish resting Y from last frame with detections using ranking
    var lastFrameWithDetections = video.Frames
        .LastOrDefault(f => f.PlateDetections != null && f.PlateDetections.Count > 0);

    if (lastFrameWithDetections == null)
    {
        Log("No detections found for resting position. Returning full duration.");
        return video.Duration;
    }

    // Match last frame detections to plate identities by rank (biggest = plate 1)
    var restingY = new Dictionary<int, float>();
    var sortedLastDetections = lastFrameWithDetections.PlateDetections
        .OrderByDescending(d => d.Height)
        .ToList();

    for (int i = 0; i < sortedLastDetections.Count && i < plates.Count; i++)
    {
        restingY[plates[i].Id] = sortedLastDetections[i].Y;
        Log($"Plate {plates[i].Id} resting Y set to {sortedLastDetections[i].Y:F1} from frame {lastFrameWithDetections.FrameNumber}");
    }

    // Scan backward to find last frame where any plate deviated from resting Y
    int lastMovementFrame = trimStartFrame;

    for (int i = video.Frames.Count - 1; i >= trimStartFrame; i--)
    {
        var frame = video.Frames[i];

        if (frame.PlateDetections == null || frame.PlateDetections.Count == 0)
            continue;

        bool movementFound = false;

        var sortedDetections = frame.PlateDetections
            .OrderByDescending(d => d.Height)
            .ToList();

        for (int j = 0; j < sortedDetections.Count && j < plates.Count; j++)
        {
            var detection = sortedDetections[j];
            var plate = plates[j];

            if (!restingY.ContainsKey(plate.Id))
                continue;

            float yDelta = Math.Abs(detection.Y - restingY[plate.Id]);
            float movementThreshold = plate.BaselineHeight / 2f;

            Log($"Frame {frame.FrameNumber} - Plate {plate.Id} Y:{detection.Y:F1} RestingY:{restingY[plate.Id]:F1} Delta:{yDelta:F1} Threshold:{movementThreshold:F1}");

            if (yDelta > movementThreshold)
            {
                lastMovementFrame = frame.FrameNumber;
                movementFound = true;
                Log($"Plate {plate.Id} still displaced at frame {frame.FrameNumber}");
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
    private void UpdatePlate(PlateIdentity plate, PlateDetection detection, int frameNumber)
    {
        plate.CurrentY = detection.Y;
        plate.CurrentX = detection.X;
        plate.CurrentHeight = detection.Height;
        plate.LastSeenFrame = frameNumber;
        Log($"Plate {plate.Id} - Y:{detection.Y:F1} Delta:{Math.Abs(detection.Y - plate.BaselineY):F1} Threshold:{plate.BaselineHeight / 2f:F1}");
    }

    private void CheckMovement(PlateIdentity plate, PlateDetection detection, int frameNumber, ref int? trimStartFrame)
    {
        float yDelta = Math.Abs(detection.Y - plate.BaselineY);
        float movementThreshold = plate.BaselineHeight / 2f;

        if (yDelta > movementThreshold && !plate.HasMoved)
        {
            plate.HasMoved = true;
            Log($"Plate {plate.Id} movement detected at frame {frameNumber}");

            if (trimStartFrame == null)
            {
                trimStartFrame = frameNumber;
                Log($"Trim start set at frame {trimStartFrame}");
            }
        }
    }
}