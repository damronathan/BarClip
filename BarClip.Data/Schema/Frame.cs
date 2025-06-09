using Microsoft.EntityFrameworkCore;
using Microsoft.ML.OnnxRuntime;
using System.ComponentModel.DataAnnotations.Schema;


namespace BarClip.Data.Schema
{
    public class Frame
    {
        public Guid Id { get; set; }
        public Guid VideoId { get; set; }
        public Video? Video { get; set; }
        public List<PlateDetection>? PlateDetections { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public int FrameNumber { get; set; }

        [NotMapped]
        public NamedOnnxValue? InputValue { get; set; }



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
            });
        }
    }
} 