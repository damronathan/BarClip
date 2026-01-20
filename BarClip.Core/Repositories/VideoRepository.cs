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

    public async Task SaveVideosAsync(SaveVideosRequest request)
    {
        // Ensure related entities are known to EF
        _context.Attach(new User { Id = request.UserId });
        _context.Attach(new Session { Id = request.SessionId });

        // Find or create the original video (and track it)
        var originalVideo = await _context.OriginalVideos
            .Include(v => v.ProcessedVideos)
            .FirstOrDefaultAsync(v => v.Id == request.OriginalVideo.Id);

        if (originalVideo == null)
        {
            originalVideo = new OriginalVideo
            {
                Id = request.OriginalVideo.Id,
                UserId = request.UserId,
                SessionId = request.SessionId,
                CreatedAt = request.OriginalVideo.UploadedAt
            };
            _context.OriginalVideos.Add(originalVideo);
        }
        else
        {
            originalVideo.TrimStart = request.OriginalVideo.TrimStart;
            originalVideo.TrimFinish = request.OriginalVideo.TrimFinish;
            originalVideo.CurrentProcessedVideoId = request.OriginalVideo.CurrentTrimmedVideoId;
        }

        // Create processed video and attach it to the tracked originalVideo
        var trimmedVideo = new ProcessedVideo
        {
            Id = request.TrimmedVideo.Id,
            UserId = request.UserId,
            OriginalVideoId = originalVideo.Id,
            Duration = request.TrimmedVideo.Duration
        };

        // If a previous one exists, replace it
        var existingTrimmedVideo = originalVideo.ProcessedVideos
            .FirstOrDefault(v => v.OriginalVideoId == trimmedVideo.OriginalVideoId);

        if (existingTrimmedVideo != null)
        {
            _context.ProcessedVideos.Remove(existingTrimmedVideo);
            await _context.SaveChangesAsync(); // flush delete first
        }

        _context.ProcessedVideos.Add(trimmedVideo);
        originalVideo.ProcessedVideos.Add(trimmedVideo);

        await _context.SaveChangesAsync();
    }

}
