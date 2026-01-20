using BarClip.Data;
using BarClip.Data.Schema;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace BarClip.Core.Repositories;

public class ExerciseRepository
{
    private readonly AppDbContext _context;

    public ExerciseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Exercise?> GetExerciseByIdAsync(Guid id)
    {
        return await _context.Exercises
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Exercise>> GetAllExercisesAsync()
    {
        return await _context.Exercises
            .ToListAsync();
    }

    //Make sure this is user specific in future before multi-user support added.
    public async Task<Exercise> VerifyExerciseExistsAsync(string exerciseName)
    {
        var exercise = await _context.Exercises
    .FirstOrDefaultAsync(e => e.ExerciseName == exerciseName);
        if (exercise is null)
        {
            return await AddExerciseAsync(exerciseName);
        }
        return exercise;
    }

    public async Task<Exercise> AddExerciseAsync(string exerciseName)
    {
        var exercise = new Exercise { ExerciseName = exerciseName, CreatedAt = DateTime.UtcNow };
        await _context.Exercises.AddAsync(exercise);
        await _context.SaveChangesAsync();
        return exercise;
    }

    

    public async Task UpdateExerciseAsync(Exercise exercise)
    {
        _context.Exercises.Update(exercise);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteExerciseAsync(Guid id)
    {
        var exercise = await GetExerciseByIdAsync(id);
        if (exercise != null)
        {
            _context.Exercises.Remove(exercise);
            await _context.SaveChangesAsync();
        }
    }
}