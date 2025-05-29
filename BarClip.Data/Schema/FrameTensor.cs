using System;

namespace BarClip.Data.Schema
{
    public class FrameTensor
    {
        public Guid Id { get; set; }
        public Guid FrameId { get; set; }
        public float[] Data { get; set; }
        public int[] Dimensions { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string ModelVersion { get; set; }
    }
} 