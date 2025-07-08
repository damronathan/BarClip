using Microsoft.AspNetCore.Http;

namespace BarClip.Models.Requests;

public class AutoTrimRequest
{
    public IFormFile VideoFile { get; set; }
}
