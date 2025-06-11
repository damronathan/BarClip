using BarClip.Data;
using BarClip.Data.Schema;
using Microsoft.EntityFrameworkCore;
using BarClip.Core.Services;

namespace BarClip.Core.Repositories;

public class VideoRepository
{
    private readonly AppDbContext _context;
    private readonly StorageService _storageService;

    public VideoRepository(AppDbContext context, StorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

    public async Task<Video?> GetOriginalVideoByTrimmedIdAsync(Guid trimmedVideoId)
    {
        return await _context.Videos.FirstOrDefaultAsync(v => v.CurrentTrimmedVideoId == trimmedVideoId);
    }

    public async Task SaveVideosAsync(Video video, TrimmedVideo trimmedVideo)
    {
        if (trimmedVideo is not null)
        {
            var existingTrimmedVideo = await _context.TrimmedVideos
                .FirstOrDefaultAsync(t => t.OriginalVideoId == trimmedVideo.OriginalVideoId && t.Id != trimmedVideo.Id);
            if (existingTrimmedVideo is null)
            {
                _context.TrimmedVideos.Add(trimmedVideo);
            }
            else
            {              
                    _context.TrimmedVideos.Remove(existingTrimmedVideo);
                    _context.TrimmedVideos.Add(trimmedVideo);              

                await _storageService.DeleteVideoAsync(existingTrimmedVideo.Id, "trimmedvideos");
            }
        }

        var existingVideo = await _context.Videos
            .FirstOrDefaultAsync(v => v.Id == video.Id);

        if (existingVideo is null)
        {
            _context.Videos.Add(video);
        }
        else
        {
            _context.Entry(existingVideo).CurrentValues.SetValues(video);
        }

        await SaveChangesAsync();
    }

}
