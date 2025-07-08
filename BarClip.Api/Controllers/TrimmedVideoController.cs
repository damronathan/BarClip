using BarClip.Core.Repositories;
using BarClip.Core.Services;
using BarClip.Models.Requests;
using BarClip.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace BarClip.Api.Controllers;

[Route("api/trimmed-videos")]
[ApiController]
[RequestSizeLimit(200_000_000)]
public class TrimmedVideoController : ControllerBase
{
    private readonly IVideoService _videoService;
    private readonly VideoRepository _videoRepository;

    public TrimmedVideoController(IVideoService videoService, VideoRepository videoRepository)
    {
        _videoService = videoService;
        _videoRepository = videoRepository;
    }

    //[HttpPost]
    //public async Task<IActionResult> TrimVideo([FromForm] AutoTrimRequest request)
    //{
    //    var url = await _videoService.TrimVideo(request.VideoFile);
    //    return Ok(new VideoResponse { SasUrl = url });
    //}

    //[HttpPost("from-source")]
    //public async Task<IActionResult> ReTrimVideo([FromForm] ManualTrimRequest request)
    //{
    //    var url = await _videoService.ReTrimVideo(request);
    //    return Ok(new VideoResponse { SasUrl = url });
    //}
    
}
