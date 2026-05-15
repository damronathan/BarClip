using BarClip.Models.Base;

namespace BarClip.Data.Schema
{
    public class Video : BaseEntity
    {
        public string VideoSasUrl { get; set; }
        public string ThumbnailSasUrl { get; set; }
    }
}
