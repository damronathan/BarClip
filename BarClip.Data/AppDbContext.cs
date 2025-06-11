using BarClip.Data.Schema;
using Microsoft.EntityFrameworkCore;
namespace BarClip.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Video> Videos { get; set; }
    public DbSet<TrimmedVideo> TrimmedVideos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        Video.Configure(modelBuilder);
        TrimmedVideo.Configure(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }
}

