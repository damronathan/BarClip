using BarClip.Core.Repositories;
using BarClip.Core.Services;
using BarClip.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BarClip.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BarClip.Api.Controllers;

[Route("api/video")]
[ApiController]
[Authorize]
public class VideoController : ControllerBase
{
    private readonly VideoRepository _repo;
    private readonly IHubContext<VideoStatusHub> _hubContext;
    public VideoController(VideoRepository repo, IHubContext<VideoStatusHub> hubContext)
    {
        _repo = repo;
        _hubContext = hubContext;
    }

    [HttpPost("save-videos")]
    public async Task<IActionResult> SaveVideos([FromBody] SaveVideosRequest request)
    {
        var url = await _repo.SaveVideosAsync(request);
        await _hubContext.Clients.User(request.UserId).SendAsync("TrimSucceeded", url);
        return Ok(new { Message = "Videos saved successfully." });
    }
}
