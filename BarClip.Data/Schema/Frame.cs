using Microsoft.EntityFrameworkCore;
using System;

namespace BarClip.Data.Schema
{
    public class Frame
    {
        public Guid Id { get; set; }
        public Guid VideoId { get; set; }
        public Video? Video { get; set; }
        public PlateDetection? PlateDetection { get; set; }
        public FrameTensor? FrameTensor { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public TimeSpan Timestamp { get; set; }
        public int FrameNumber { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsProcessed { get; set; }



        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Frame>(entity =>
            {
                entity.HasKey(f => f.Id);
                entity.HasOne(f => f.Video)
                    .WithMany(v => v.Frames)
                    .HasForeignKey(f => f.VideoId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(f => f.PlateDetection)
                      .WithOne(pd => pd.Frame)
                      .HasForeignKey<PlateDetection>(pd => pd.FrameId)
                      .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(f => f.FrameTensor)
                      .WithOne(ft => ft.Frame)
                      .HasForeignKey<FrameTensor>(ft => ft.FrameId)
                      .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
} 