using BarClip.Models.Domain;
using System.Text.Json.Serialization;

namespace BarClip.Models.Requests
{
    public class OriginalVideoRequest
    {
        public Guid Id { get; set; }
        public TimeSpan TrimStart { get; set; }
        public TimeSpan TrimFinish { get; set; }
        public Guid CurrentTrimmedVideoId { get; set; }
        public LifterFilter LifterFilter { get; set; } = LifterFilter.Whole;
        public int LiftNumber { get; set; }

        public string ThumbnailPath { get; set; } = null!;

        public double? WeightKg { get; set; } = 0;

        public TimeSpan Duration { get; set; }
        public List<Frame>? Frames { get; set; } = [];

        public string? FilePath { get; set; } = null!;
        public string? CompressedPath { get; set; } = null!;
    }
    public enum LifterFilter
    {
        Whole,
        Left,
        Right
    }
}
