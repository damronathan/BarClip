using Microsoft.EntityFrameworkCore;
using System;

namespace BarClip.Data.Schema
{
    public class Frame
    {
        public Guid Id { get; set; }
        public Guid VideoId { get; set; }
        public Video? Video { get; set; }
        public List<PlateDetection>? PlateDetections { get; set; }
        public FrameTensor? FrameTensor { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public TimeSpan Timestamp { get; set; }
        public int FrameNumber { get; set; }
        public int[] Dimensions { get; set; } = [1, 3, 640, 640];
        public string Name { get; set; } = "images";




        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Frame>(entity =>
            {
                entity.HasKey(f => f.Id);
                entity.HasOne(f => f.Video)
                    .WithMany(v => v.Frames)
                    .HasForeignKey(f => f.VideoId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasMany(f => f.PlateDetections)
                      .WithOne(pd => pd.Frame)
                      .HasForeignKey(pd => pd.FrameId)
                      .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(f => f.FrameTensor)
                      .WithOne(ft => ft.Frame)
                      .HasForeignKey<FrameTensor>(ft => ft.FrameId)
                      .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
} 