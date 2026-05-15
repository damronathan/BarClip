using BarClip.Core.Services;
using BarClip.Data;
using BarClip.Data.Schema;
using BarClip.Models.Requests;
using BarClip.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace BarClip.Core.Repositories;

public class VideoRepository
{
    private readonly AppDbContext _context;

    public VideoRepository(AppDbContext context)
    {
        _context = context;
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
    public async Task<ICollection<Video>> GetAllVideosAsync()
    {
        return await _context.Videos
            .ToListAsync();
    }
    public async Task UpsertVideosAsync(IEnumerable<VideoResponse> videos)
    {
        foreach (var video in videos)
        {
            var existing = await _context.Videos.FindAsync(video.Id);
            if (existing == null)
            {
                _context.Videos.Add(new Video
                {
                    Id = video.Id,
                    VideoSasUrl = video.VideoSasUrl,
                    ThumbnailSasUrl = video.ThumbnailSasUrl
                });
            }
            else
            {
                existing.VideoSasUrl = video.VideoSasUrl;
                existing.ThumbnailSasUrl = video.ThumbnailSasUrl;
            }
        }

        await _context.SaveChangesAsync();
    }

}
