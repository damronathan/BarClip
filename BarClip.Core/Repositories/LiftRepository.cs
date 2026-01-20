using BarClip.Data;
using BarClip.Data.Schema;
using Microsoft.EntityFrameworkCore;

namespace BarClip.Core.Repositories;
public class LiftRepository
{
    private readonly AppDbContext _context;

    public LiftRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddLiftAsync(Lift lift)
    {
        await _context.Lifts.AddAsync(lift);
        await _context.SaveChangesAsync();
    }
    public async Task<Lift?> GetLiftByIdAsync(Guid Id)
    {
        var lift = await _context.Lifts
        .FirstOrDefaultAsync(l => l.Id == Id);
        return lift;
    }
    public async Task<Lift?> GetLiftByOriginalVideoIdAsync(Guid originalVideoId)
    {
        var lift = await _context.Lifts
        .FirstOrDefaultAsync(l => l.OriginalVideoId == originalVideoId);
        return lift;
    }

    public async Task<Exercise?> GetExerciseByNameAsync(string exerciseName)
    {
        return await _context.Exercises
            .FirstOrDefaultAsync(e => e.ExerciseName == exerciseName);
    }
    public async Task AddExerciseAsync(Exercise exercise)
    {
        await _context.Exercises.AddAsync(exercise);
        await _context.SaveChangesAsync();
    }
    public async Task UpdateLiftAsync(Lift lift)
    {
        _context.Lifts.Update(lift);
        await _context.SaveChangesAsync();
    }
    public async Task UpdateLiftOnlyAsync(Lift lift)
    {
        _context.Lifts.Attach(lift);
        _context.Entry(lift).Property(l => l.WeightKg).IsModified = true;
        _context.Entry(lift).Property(l => l.LifterFilter).IsModified = true;
        await _context.SaveChangesAsync();
    }


}
