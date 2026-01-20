using BarClip.Data.Schema;
using Microsoft.EntityFrameworkCore;
namespace BarClip.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<OriginalVideo> OriginalVideos { get; set; }
    public DbSet<ProcessedVideo> ProcessedVideos { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<Lift> Lifts { get; set; }
    public DbSet<Session> Sessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OriginalVideo.Configure(modelBuilder);
        ProcessedVideo.Configure(modelBuilder);
        User.Configure(modelBuilder);
        Exercise.Configure(modelBuilder);
        Lift.Configure(modelBuilder);
        Session.Configure(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }
}

