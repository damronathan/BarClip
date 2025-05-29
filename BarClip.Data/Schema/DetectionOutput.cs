using System;

namespace BarClip.Data.Schema
{
    public class DetectionOutput
    {
        public Guid Id { get; set; }
        public Guid FrameTensorId { get; set; }
        public float[] RawOutput { get; set; }
        public string OutputName { get; set; }
        public int[] OutputShape { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string ModelVersion { get; set; }
    }
} 