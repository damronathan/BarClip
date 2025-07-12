using BarClip.Core.Repositories;
using BarClip.Core.Services;
using BarClip.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BarClip.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace BarClip.Api.Controllers;

[Route("api/video")]
[ApiController]
[Authorize]
public class VideoController : ControllerBase
{
    private readonly IVideoDataService _service;
    private readonly IHubContext<VideoStatusHub> _hubContext;
    public VideoController(IVideoDataService service, IHubContext<VideoStatusHub> hubContext)
    {
        _service = service;
        _hubContext = hubContext;
    }

    [HttpPost("save-videos")]
    public async Task<IActionResult> SaveVideos([FromBody] SaveVideosRequest request)
    {
        var url = _service.GetDownloadSasUrl(request.TrimmedVideo.Id);

        if (string.IsNullOrEmpty(url))
        {
            return BadRequest("Failed to generate download SAS URL");
        }

        await _hubContext.Clients.User(request.UserId).SendAsync("TrimSucceeded", url);

        await _service.SaveVideos(request);

        return Ok(new { Message = "Videos saved successfully." });
    }

    [HttpGet("upload-sas-url")]
    public UploadSasUrlResponse UploadSasUrl()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("User identification not found");
        }

        var url = _service.GetUploadSasUrl(Guid.NewGuid());

        if (string.IsNullOrEmpty(url))
        {
            throw new InvalidOperationException("Failed to generate upload SAS URL");
        }
        var response = new UploadSasUrlResponse
        {
            UserId = userId,
            UploadSasUrl = url
        };
        return response;
    }
}
