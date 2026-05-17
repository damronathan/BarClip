namespace BarClip.Models.Requests;

public class UploadSasUrlResponse
{
    public required string UserId { get; set; }
    public required string VideoSasUrl { get; set; }
    public required string ThumbnailSasUrl { get; set; }

}
