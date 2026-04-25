namespace BarClip.Models.Requests
{
    public class GetVideosRequest
    {
        public Guid? SessionId { get; set;  }
        public Guid? UserId { get; set; }
        }
}
