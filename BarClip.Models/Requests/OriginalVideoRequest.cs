using BarClip.Models.Domain;
using System.Text.Json.Serialization;

namespace BarClip.Models.Requests
{
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
        public TimeSpan Duration { get; set; }
        [JsonIgnore]
        public List<Frame>? Frames { get; set; } = [];

        [JsonIgnore]
        public string? FilePath { get; set; } = null!;
        [JsonIgnore]
        public string? CompressedPath { get; set; } = null!;
    }
    public enum LifterFilter
    {
        Whole,
        Left,
        Right
    }
}
