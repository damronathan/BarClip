using System;

namespace BarClip.Data.Schema
{
    public class PlateDetection
    {
        public Guid Id { get; set; }
        public Guid DetectionOutputId { get; set; }
        public float Confidence { get; set; }
        public float[] BoundingBox { get; set; } // [x1, y1, x2, y2]
        public string PlateText { get; set; }
        public DateTime DetectedAt { get; set; }
        public bool IsVerified { get; set; }
        public string VerificationNotes { get; set; }
    }
} 