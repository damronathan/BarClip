using Microsoft.AspNetCore.Http;

namespace BarClip.Models.Requests;

public class TrimVideoRequest
{
    public Guid Id { get; set; }
    public IFormFile VideoFile { get; set; }
}
