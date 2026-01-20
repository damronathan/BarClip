using BarClip.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace BarClip.Data.Schema;

public class Session : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string? Title { get; set; }
    public List<Lift>? Lifts { get; set; }
    public List<OriginalVideo>? OriginalVideos { get; set; }
    public double? Duration { get; set; }
    public int? RepsMade { get; set; }
    public int? RepsMissed { get; set; }

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.HasOne(s => s.User)
                  .WithMany(u => u.Sessions)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(s => s.Lifts)
                  .WithOne(l => l.Session)
                  .HasForeignKey(l => l.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(s => s.OriginalVideos)
                  .WithOne(ov => ov.Session)
                  .HasForeignKey(l => l.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}