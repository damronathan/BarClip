using BarClip.Models.Base;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarClip.Data.Schema;

public class OriginalVideo : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid SessionId { get; set; }
    public Session? Session { get; set; }
    public Lift? Lift { get; set; }
    public TimeSpan TrimStart { get; set; }
    public TimeSpan TrimFinish { get; set; }
    public List<ProcessedVideo>? ProcessedVideos { get; set; }
    public Guid? CurrentProcessedVideoId { get; set; }

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OriginalVideo>(entity =>
        {
            entity.HasKey(v => v.Id);

            entity.HasMany(v => v.ProcessedVideos)
                  .WithOne(t => t.OriginalVideo)
                  .HasForeignKey(t => t.OriginalVideoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(o => o.User)
                .WithMany(u => u.OriginalVideos)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(o => o.Session)
                  .WithMany(s => s.OriginalVideos)
                  .HasForeignKey(l => l.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}