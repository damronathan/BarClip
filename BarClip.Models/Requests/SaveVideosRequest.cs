using BarClip.Models.Domain;
using FFMpegCore;
using System.Reflection.Metadata;
using System.Text.Json.Serialization;

namespace BarClip.Models.Requests;
public class SaveVideosRequest
{
    public OriginalVideoRequest OriginalVideo { get; set; }
    public TrimmedVideoRequest TrimmedVideo { get; set; }
    public string UserId { get; set; }
}
public class OriginalVideoRequest
{
    public Guid Id { get; set; }
    public DateTime UploadedAt { get; set; }
    public TimeSpan TrimStart { get; set; }
    public TimeSpan TrimFinish { get; set; }
    public Guid CurrentTrimmedVideoId { get; set; }

    [JsonIgnore]
    public IMediaAnalysis? VideoAnalysis { get; set; }
    [JsonIgnore]
    public List<Frame>? Frames { get; set; } = [];

    [JsonIgnore]
    public string? FilePath { get; set; } = null!;


}
public class TrimmedVideoRequest
{
    public Guid Id { get; set; }
    public TimeSpan Duration { get; set; }

    [JsonIgnore]
    public string? FilePath { get; set; }

}
