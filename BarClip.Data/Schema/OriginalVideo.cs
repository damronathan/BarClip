using Microsoft.EntityFrameworkCore;

namespace BarClip.Data.Schema
{
    public class OriginalVideo
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public required User User { get; set; }
        public DateTime UploadedAt { get; set; }
        public TimeSpan TrimStart { get; set; }
        public TimeSpan TrimFinish { get; set; }
        public List<TrimmedVideo>? TrimmedVideos { get; set; }
        public Guid CurrentTrimmedVideoId { get; set; }

        
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OriginalVideo>(entity =>
            {
                entity.HasKey(v => v.Id);

                entity.HasMany(v => v.TrimmedVideos)
                      .WithOne(t => t.OriginalVideo)
                      .HasForeignKey(t => t.OriginalVideoId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(o => o.User)
                    .WithMany(u => u.OriginalVideos)
                    .HasForeignKey(o => o.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
