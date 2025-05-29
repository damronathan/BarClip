using BarClip.Data.Schema;
using Microsoft.EntityFrameworkCore;
namespace BarClip.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Video> Videos { get; set; }
    public DbSet<VideoConfiguration> VideoConfigurations { get; set; }
    public DbSet<TrimmedVideo> TrimmedVideos { get; set; }
    public DbSet<PlateDetection> PlateDetections { get; set; }
    public DbSet<FrameTensor> FrameTensors { get; set; }
    public DbSet<Frame> Frames { get; set; }
    public DbSet<DetectionOutput> DetectionOutputs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        User.Configure(modelBuilder);
        Video.Configure(modelBuilder);
        TrimmedVideo.Configure(modelBuilder);
        VideoConfiguration.Configure(modelBuilder);
        PlateDetection.Configure(modelBuilder);
        FrameTensor.Configure(modelBuilder);
        Frame.Configure(modelBuilder);
        DetectionOutput.Configure(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }
}

