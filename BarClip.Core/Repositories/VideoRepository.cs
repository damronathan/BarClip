using BarClip.Data;
using BarClip.Data.Schema;
using Microsoft.EntityFrameworkCore;
using BarClip.Core.Services;
using BarClip.Models.Requests;

namespace BarClip.Core.Repositories;

public class VideoRepository
{
    private readonly AppDbContext _context;
    private readonly StorageService _storageService;
    private readonly UserRepository _userRepository;

    public VideoRepository(AppDbContext context, StorageService storageService, UserRepository userRepository)
    {
        _context = context;
        _storageService = storageService;
        _userRepository = userRepository;
    }
    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    public async Task AddProcessedVideoAsync(ProcessedVideo processed)
    {
        // Clear navigation properties to avoid tracking issues
        processed.User = null;
        processed.OriginalVideo = null;

        // Verify foreign keys are set
        if (processed.UserId == Guid.Empty)
            throw new InvalidOperationException("ProcessedVideo.UserId cannot be empty");

        if (processed.OriginalVideoId == Guid.Empty)
            throw new InvalidOperationException("ProcessedVideo.OriginalVideoId cannot be empty");

        _context.ProcessedVideos.Add(processed);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateOriginalVideoAsync(OriginalVideo original)
    {
        // Fetch the existing entity and update only specific properties
        var existing = await _context.OriginalVideos.FindAsync(original.Id);

        if (existing == null)
            throw new InvalidOperationException($"OriginalVideo {original.Id} not found");

        existing.TrimStart = original.TrimStart;
        existing.TrimFinish = original.TrimFinish;
        existing.CurrentProcessedVideoId = original.CurrentProcessedVideoId;

        await _context.SaveChangesAsync();
    }
    public async Task<List<OriginalVideo>> GetOriginalVideosForSessionAsync(Guid sessionId)
    {
        return await _context.OriginalVideos
            .Where(v => v.SessionId == sessionId)
                    .AsNoTracking()
            .ToListAsync();
    }

    public async Task<OriginalVideo> CreateOriginalVideoAsync(OriginalVideo video)
    {
        _context.OriginalVideos.Add(video);
        await _context.SaveChangesAsync();
        return video;
    }
    public async Task<OriginalVideo?> GetOriginalVideoByTrimmedIdAsync(Guid trimmedVideoId)
    {
        return await _context.OriginalVideos
            .FirstOrDefaultAsync(v => v.CurrentProcessedVideoId == trimmedVideoId);
    }

}
