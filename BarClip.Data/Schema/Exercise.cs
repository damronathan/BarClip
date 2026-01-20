using BarClip.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace BarClip.Data.Schema;

public class Exercise : BaseEntity
{
    public string? ExerciseName { get; set; }
    public ICollection<Lift>? Lifts { get; set; } = [];

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasMany(e => e.Lifts)
                  .WithOne(l => l.Exercise)
                  .HasForeignKey(l => l.ExerciseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}