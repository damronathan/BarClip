using System;

namespace BarClip.Data.Schema
{
    public class Frame
    {
        public Guid Id { get; set; }
        public Guid VideoId { get; set; }
        public string FilePath { get; set; }
        public TimeSpan Timestamp { get; set; }
        public int FrameNumber { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime ExtractedAt { get; set; }
        public bool IsProcessed { get; set; }
    }
} 