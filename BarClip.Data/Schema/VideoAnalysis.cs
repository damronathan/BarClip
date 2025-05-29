using System;
using System.Collections.Generic;

namespace BarClip.Data.Schema
{
    public class VideoAnalysis
    {
        public Guid Id { get; set; }
        public Guid VideoId { get; set; }
        public DateTime AnalysisStartedAt { get; set; }
        public DateTime? AnalysisCompletedAt { get; set; }
        public AnalysisStatus Status { get; set; }
        public int TotalFrames { get; set; }
        public int ProcessedFrames { get; set; }
        public int DetectedPlates { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> AnalysisMetadata { get; set; }
    }

    public enum AnalysisStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }
} 