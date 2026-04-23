namespace BarClip.Models.Requests
{
    public class UploadVideoRequest
    {
        public Stream Content { get; set; }
        public string ContentType { get; set; }
        public string UserId { get; set; }
        public string SasUrl { get; set;  }
        public Guid VideoId { get; set; }
        public Guid SessionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int OrderNumber { get; set; }
        public bool IsFull { get; set; }
    }

}
