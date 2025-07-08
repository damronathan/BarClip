using Microsoft.EntityFrameworkCore;

namespace BarClip.Data.Schema;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string AzureB2CId { get; set; }
    public List<TrimmedVideo>? TrimmedVideos { get; set; }
    public List<OriginalVideo>? OriginalVideos { get; set; }
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(v => v.Id);

            entity.HasMany(v => v.TrimmedVideos)
                  .WithOne(t => t.User)
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(v => v.OriginalVideos)
                  .WithOne(t => t.User)
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

