using BarClip.Core.Repositories;
using BarClip.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BarClip.Api.Controllers
{
    [Route("api/video")]
    [ApiController]
    [Authorize]
    public class VideoController : ControllerBase
    {
        VideoRepository _videoRepository;
        public VideoController(VideoRepository videoRepository)
        {
            _videoRepository = videoRepository;
        }

        [HttpPost("save-videos")]
        public async Task<IActionResult> SaveVideos([FromBody] SaveVideosRequest request)
        {
            await _videoRepository.SaveVideosAsync(request);
            return Ok(new { Message = "Videos saved successfully." });
        }
    }
}
