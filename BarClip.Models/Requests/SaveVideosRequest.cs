using BarClip.Models.Domain;
using FFMpegCore;
using System.Reflection.Metadata;
using System.Text.Json.Serialization;

namespace BarClip.Models.Requests;
public class SaveVideosRequest
{
    public OriginalVideoRequest OriginalVideo { get; set; }
    public ProcessedVideoRequest TrimmedVideo { get; set; }
    public Guid UserId { get; set; }
    public string EntraId { get; set; }
    public Guid SessionId { get; set; }
}
public class OriginalVideoRequest
{
    public Guid Id { get; set; }
    public DateTime UploadedAt { get; set; }
    public TimeSpan TrimStart { get; set; }
    public TimeSpan TrimFinish { get; set; }
    public Guid CurrentTrimmedVideoId { get; set; }
    public LifterFilter LifterFilter { get; set; } = LifterFilter.Whole;
    public Guid UserId { get; set; }
    public int LiftNumber { get; set; }

    // Additional UI properties
    [JsonIgnore]
    public string ThumbnailPath { get; set; } = null!;

    [JsonIgnore]
    public double? WeightKg { get; set; } = 0;

    [JsonIgnore]
    public IMediaAnalysis? VideoAnalysis { get; set; }
    [JsonIgnore]
    public List<Frame>? Frames { get; set; } = [];

    [JsonIgnore]
    public string? FilePath { get; set; } = null!;
}

public class ProcessedVideoRequest
{
    public Guid Id { get; set; }
    public TimeSpan Duration { get; set; }

    [JsonIgnore]
    public string? FilePath { get; set; }

}

public enum LifterFilter
{
    Whole,
    Left,
    Right
}
