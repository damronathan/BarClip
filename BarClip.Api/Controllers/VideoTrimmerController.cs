using BarClip.Core.Services;
using BarClip.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace BarClip.Api.Controllers;

[Route("api/video-trimmer")]
[ApiController]

public class VideoTrimmerController(VideoProcessorService service) : ControllerBase
{
    public async Task<IActionResult> TrimVideo([FromForm] TrimVideoRequest request)
    {
        await VideoProcessorService.TrimOriginalVideo(request.Id.ToString());

    }

}
