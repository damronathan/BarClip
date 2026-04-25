using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarClip.Models.Responses;

public class VideoResponse
{
    public Guid Id { get; set; }
    public int OrderNumber { get; set; }
    public bool IsFull { get; set; }
    public string? VideoSasUrl { get; set; }
    public string? ThumbnailSasUrl { get; set; }
}
