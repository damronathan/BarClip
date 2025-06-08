using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace BarClip.Data.Schema
{
    public class PlateDetection
    {
        public Guid Id { get; set; }
        public Guid FrameId { get; set; }
        public Frame? Frame { get; set; }
        public float Confidence { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public int DetectionNumber { get; set; }

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlateDetection>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.HasOne(p => p.Frame)
                    .WithMany(f => f.PlateDetections)
                    .HasForeignKey(p => p.FrameId)
                    .OnDelete(DeleteBehavior.NoAction);  
            });
        }
    }
}
