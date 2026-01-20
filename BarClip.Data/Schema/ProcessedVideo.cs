using BarClip.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace BarClip.Data.Schema;

public class ProcessedVideo : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid OriginalVideoId { get; set; }
    public OriginalVideo? OriginalVideo { get; set; }
    public TimeSpan Duration { get; set; }

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedVideo>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.HasOne(t => t.OriginalVideo)
                .WithMany(v => v.ProcessedVideos)
                .HasForeignKey(t => t.OriginalVideoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.User)
                .WithMany(u => u.TrimmedVideos)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}