using BarClip.Models.Base;
using BarClip.Models.Requests;
using Microsoft.EntityFrameworkCore;

namespace BarClip.Data.Schema;

public class Lift : BaseEntity
{
    public Guid SessionId { get; set; }
    public Session Session { get; set; }
    public double? WeightKg { get; set; }
    public int? Reps { get; set; } = 1;
    public bool Successful { get; set; } = true;
    public Guid OriginalVideoId { get; set; }
    public OriginalVideo? OriginalVideo { get; set; }
    public Guid? ProcessedVideoId { get; set; }
    public ProcessedVideo? ProcessedVideo { get; set; }
    public Guid? ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }
    public LifterFilter LifterFilter { get; set; }

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Lift>(entity =>
        {
            entity.HasKey(l => l.Id);

            entity.HasOne(l => l.Session)
                  .WithMany(s => s.Lifts)
                  .HasForeignKey(l => l.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.OriginalVideo)
                   .WithOne(v => v.Lift)
                   .HasForeignKey<Lift>(l => l.OriginalVideoId)
                   .OnDelete(DeleteBehavior.Cascade);


            entity.HasOne(l => l.ProcessedVideo)
                  .WithMany()
                  .HasForeignKey(l => l.ProcessedVideoId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.Exercise)
                  .WithMany(e => e.Lifts)
                  .HasForeignKey(l => l.ExerciseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}