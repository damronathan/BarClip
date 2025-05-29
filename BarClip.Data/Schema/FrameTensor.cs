using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace BarClip.Data.Schema
{
    public class FrameTensor
    {
        public Guid Id { get; set; }
        public Guid FrameId { get; set; }
        public Frame? Frame { get; set; }
        public List<DetectionOutput> DetectionOutputs { get; set; } = new();
        public float[] Data { get; set; } = Array.Empty<float>();
        public int[] Dimensions { get; set; } = Array.Empty<int>();
        public DateTime ProcessedAt { get; set; }
        public string ModelVersion { get; set; } = string.Empty;

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FrameTensor>(entity =>
            {
                entity.HasKey(ft => ft.Id);
                entity.HasOne(ft => ft.Frame)
                    .WithOne(f => f.FrameTensor)
                    .HasForeignKey<FrameTensor>(ft => ft.FrameId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasMany(ft => ft.DetectionOutputs)
                    .WithOne(d => d.FrameTensor)
                    .HasForeignKey(d => d.FrameTensorId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
