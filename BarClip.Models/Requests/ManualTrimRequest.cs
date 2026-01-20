using Microsoft.AspNetCore.Http;

namespace BarClip.Models.Requests;

public class ManualTrimRequest
{
    public IFormFile? TrimmedVideoFile { get; set; }
    public Guid Id { get; set; }
    public bool StartEarlier { get; set; }
    public bool FinishEarlier { get; set; }
    public double TrimStart { get; set; }
    public double TrimFinish { get; set; }
}
