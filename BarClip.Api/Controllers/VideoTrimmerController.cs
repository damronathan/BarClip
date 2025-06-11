using BarClip.Core.Services;
using BarClip.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace BarClip.Api.Controllers;

[Route("api/video-trimmer")]
[ApiController]
[RequestSizeLimit(200_000_000)]
public class VideoTrimmerController : ControllerBase
{
    private readonly VideoService _videoService;
    private readonly StorageService _storageService;

    public VideoTrimmerController(VideoService videoService, StorageService storageService)
    {
        _videoService = videoService;
        _storageService = storageService;
    }

    [HttpPost("trim-video")]
    public async Task<IActionResult> TrimVideo([FromForm] TrimVideoRequest request)
    {
        var trimmedVideo = await _videoService.TrimOriginalVideo(request.VideoFile);
        var url = _storageService.GenerateSasUrl(trimmedVideo.Id);
        return Ok(new { sasUrl = url });
    }

    [HttpPost("re-trim-video")]
    public async Task<IActionResult> ReTrimVideo([FromForm] ReTrimVideoRequest request)
    {
        var trimmedVideo = await _videoService.ReTrimOriginalVideo(request);
        var url = _storageService.GenerateSasUrl(trimmedVideo.Id);
        return Ok(new { sasUrl = url });
    }
}
