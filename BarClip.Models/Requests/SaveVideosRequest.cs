using BarClip.Models.Domain;
using FFMpegCore;
using System.Reflection.Metadata;

namespace BarClip.Models.Requests;
public class SaveVideosRequest
{
    public OriginalVideoRequest OriginalVideo { get; set; }
    public TrimmedVideoRequest TrimmedVideo { get; set; }
    public Guid UserId { get; set; }
}
public class OriginalVideoRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public DateTime UploadedAt { get; set; }
    public TimeSpan TrimStart { get; set; }
    public TimeSpan TrimFinish { get; set; }
    public List<TrimmedVideoRequest>? TrimmedVideos { get; set; }
    public Guid CurrentTrimmedVideoId { get; set; }
    public IMediaAnalysis? VideoAnalysis { get; set; }
    public List<Frame> Frames { get; set; } = [];
    public string FilePath { get; set; } = null!;


}
public class TrimmedVideoRequest
{
    public Guid Id { get; set; }
    public Guid OriginalVideoId { get; set; }
    public OriginalVideoRequest? OriginalVideo { get; set; }
    public TimeSpan Duration { get; set; }
    public string? FilePath { get; set; }

}
