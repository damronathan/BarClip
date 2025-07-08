using BarClip.Data.Schema;
using Microsoft.EntityFrameworkCore;
namespace BarClip.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<OriginalVideo> OriginalVideos { get; set; }
    public DbSet<TrimmedVideo> TrimmedVideos { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OriginalVideo.Configure(modelBuilder);
        TrimmedVideo.Configure(modelBuilder);
        User.Configure(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }
}

