using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace BarClip.Data.Schema
{
    public class PlateDetection
    {
        public Guid Id { get; set; }

        public Guid DetectionOutputId { get; set; }
        public DetectionOutput? DetectionOutput { get; set; }

        public Guid FrameId { get; set; }
        public Frame? Frame { get; set; }

        public Guid VideoConfigurationId { get; set; }
        public VideoConfiguration? VideoConfiguration { get; set; }

        public float Confidence { get; set; }
        public float[] BoundingBox { get; set; } = Array.Empty<float>(); // [x1, y1, x2, y2]
        public string PlateText { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
        public bool IsVerified { get; set; }
        public string VerificationNotes { get; set; } = string.Empty;

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlateDetection>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.HasOne(p => p.DetectionOutput)
                    .WithMany(d => d.PlateDetections)
                    .HasForeignKey(p => p.DetectionOutputId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(p => p.Frame)
                    .WithOne(f => f.PlateDetection)
                    .HasForeignKey<PlateDetection>(p => p.FrameId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(p => p.VideoConfiguration)
                    .WithMany(v => v.PlateDetections)
                    .HasForeignKey(p => p.VideoConfigurationId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
