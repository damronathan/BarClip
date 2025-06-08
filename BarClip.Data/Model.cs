using BarClip.Data.Schema;
using Microsoft.EntityFrameworkCore;
namespace BarClip.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Video> Videos { get; set; }
    public DbSet<TrimmedVideo> TrimmedVideos { get; set; }
    public DbSet<PlateDetection> PlateDetections { get; set; }
    public DbSet<Frame> Frames { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        User.Configure(modelBuilder);
        Video.Configure(modelBuilder);
        TrimmedVideo.Configure(modelBuilder);
        PlateDetection.Configure(modelBuilder);
        Frame.Configure(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }
}

