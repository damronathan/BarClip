using System;

namespace BarClip.Data.Schema
{
    public class Video
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FilePath { get; set; }
        public TimeSpan Duration { get; set; }
        public double FrameRate { get; set; }
        public DateTime UploadedAt { get; set; }
        public VideoStatus Status { get; set; }
    }

    public enum VideoStatus
    {
        Uploaded,
        Processing,
        Trimmed,
        Failed
    }
} 