using FFMpegCore;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using BarClip.Models.Domain;

namespace BarClip.Data.Schema
{
    public class Video
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public DateTime UploadedAt { get; set; }
        public TimeSpan TrimStart { get; set; }
        public TimeSpan TrimFinish { get; set; }
        public List<TrimmedVideo>? TrimmedVideos { get; set; }
        public Guid CurrentTrimmedVideoId { get; set; }

        [NotMapped]

        public IMediaAnalysis? VideoAnalysis { get; set; }
        [NotMapped]

        public List<Frame> Frames { get; set; } = [];
        [NotMapped]

        public string FilePath { get; set; } = null!;



        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Video>(entity =>
            {
                entity.HasKey(v => v.Id);

                entity.HasMany(v => v.TrimmedVideos)
                      .WithOne(t => t.OriginalVideo)
                      .HasForeignKey(t => t.OriginalVideoId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
