using Microsoft.EntityFrameworkCore;
using System;

namespace BarClip.Data.Schema
{
    public class DetectionOutput
    {
        public Guid Id { get; set; }
        public Guid FrameTensorId { get; set; }
        public FrameTensor FrameTensor { get; set; }
        public List<PlateDetection>? PlateDetections { get; set; }
        public float[]? RawOutput { get; set; }
        public string? OutputName { get; set; }
        public int[]? OutputShape { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string? ModelVersion { get; set; }



        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DetectionOutput>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.HasOne(d => d.FrameTensor)
                    .WithMany(ft => ft.DetectionOutputs)
                    .HasForeignKey(d => d.FrameTensorId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasMany(d => d.PlateDetections)
                    .WithOne(p => p.DetectionOutput)
                    .HasForeignKey(p => p.DetectionOutputId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
} 