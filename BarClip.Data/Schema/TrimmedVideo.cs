using System;

namespace BarClip.Data.Schema
{
    public class TrimmedVideo
    {
        public Guid Id { get; set; }
        public Guid OriginalVideoId { get; set; }
        public string FilePath { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; }
        public bool IsProcessed { get; set; }
    }
} 