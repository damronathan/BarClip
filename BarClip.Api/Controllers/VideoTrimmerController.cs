using BarClip.Core.Services;
using BarClip.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace BarClip.Api.Controllers;

[Route("api/video-trimmer")]
[ApiController]

public class VideoTrimmerController(VideoService service)   : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> TrimVideo([FromForm] TrimVideoRequest request)
    {
        await VideoService.TrimOriginalVideo(request.VideoFile);
        return Ok(new { success = true });
    }

}
