using BarClip.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace BarClip.Data.Schema;

public class User : BaseEntity
{
    public string EntraId { get; set; }
    public string? Email { get; set; }
    public List<Session>? Sessions { get; set; }
    public List<ProcessedVideo>? TrimmedVideos { get; set; }
    public List<OriginalVideo>? OriginalVideos { get; set; }

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.HasMany(u => u.Sessions)
                  .WithOne(s => s.User)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.TrimmedVideos)
                  .WithOne(t => t.User)
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(u => u.OriginalVideos)
                  .WithOne(o => o.User)
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}