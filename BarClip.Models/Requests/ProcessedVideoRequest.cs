using System.Text.Json.Serialization;

namespace BarClip.Models.Requests
{
    public class ProcessedVideoRequest
    {
        public Guid Id { get; set; }
        public TimeSpan Duration { get; set; }

        [JsonIgnore]
        public string? FilePath { get; set; }

    }
}
