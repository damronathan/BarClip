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

    public async Task<OriginalVideo?> GetOriginalVideoByTrimmedIdAsync(Guid trimmedVideoId)
    {
        return await _context.OriginalVideos
            .FirstOrDefaultAsync(v => v.CurrentTrimmedVideoId == trimmedVideoId);
    }

    public async Task<string> SaveVideosAsync(SaveVideosRequest request)
    {
        var user = await _userRepository.GetUserByIdAsync(request.UserId);

        var originalVideo = new OriginalVideo
        {
            Id = request.OriginalVideo.Id,
            UserId = request.UserId,
            User = user,
            UploadedAt = request.OriginalVideo.UploadedAt,
            TrimStart = request.OriginalVideo.TrimStart,
            TrimFinish = request.OriginalVideo.TrimFinish,
            CurrentTrimmedVideoId = request.OriginalVideo.CurrentTrimmedVideoId,
            TrimmedVideos = []
        };

        var trimmedVideo = new TrimmedVideo { 
            Id = request.TrimmedVideo.Id,
            UserId = request.UserId,
            User = user,
            OriginalVideoId = request.OriginalVideo.Id, 
            OriginalVideo = originalVideo, 
            Duration = request.TrimmedVideo.Duration 
        };

        originalVideo.TrimmedVideos.Add(trimmedVideo);

        if (trimmedVideo is not null)
        {
            var existingTrimmedVideo = await _context.TrimmedVideos
                .FirstOrDefaultAsync(t =>
                    t.OriginalVideoId == trimmedVideo.OriginalVideoId &&
                    t.Id != trimmedVideo.Id);

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

        var existingVideo = await _context.OriginalVideos
            .FirstOrDefaultAsync(v => v.Id == originalVideo.Id);

        if (existingVideo is null)
        {
            _context.OriginalVideos.Add(originalVideo);
        }
        else
        {
            _context.Entry(existingVideo).CurrentValues.SetValues(originalVideo);
        }

        await SaveChangesAsync();

        return _storageService.GenerateDownloadSasUrl(request.TrimmedVideo.Id);
    }
}
