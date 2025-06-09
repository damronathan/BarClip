using Microsoft.AspNetCore.Http;

namespace BarClip.Models.Requests;

public class TrimVideoRequest
{
    public IFormFile VideoFile { get; set; }
}
