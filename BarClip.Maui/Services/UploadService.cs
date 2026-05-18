using BarClip.Core.Services;
using BarClip.Models.Requests;

namespace BarClip.Maui.Services
{
    public class UploadService
    {
        private readonly ApiClientService _client;
        private readonly IVideoService _videoService;

        public UploadService(ApiClientService client, IVideoService videoService)
        {
            _client = client;
            _videoService = videoService;
        }

        public async Task UploadVideo(Guid sessionId, string sessionPath, string thumbnailPath)
        {
            var request = new SasUrlRequest()
            {
                Id = sessionId,
            };

            var response = await _client.GetUploadSasUrlAsync(request);

            if (!File.Exists(sessionPath))
                throw new Exception($"Video file not found: {sessionPath}");

            if (!File.Exists(thumbnailPath))
                throw new Exception($"Thumbnail file not found: {thumbnailPath}");

            var uploadVideoRequest = new UploadRequest()
            {
                Content = File.OpenRead(sessionPath),
                ContentType = "video/quicktime",
                UserId = response.UserId,
                SasUrl = response.VideoSasUrl,
                VideoId = sessionId,
                SessionId = sessionId,
                CreatedAt = DateTime.UtcNow,
                OrderNumber = 1,
                IsFull = true
            };

            var uploadThumbnailRequest = new UploadRequest()
            {
                Content = File.OpenRead(thumbnailPath),
                ContentType = "image/jpeg",
                UserId = response.UserId,
                SasUrl = response.ThumbnailSasUrl,
                VideoId = sessionId,
                SessionId = sessionId,
                CreatedAt = DateTime.UtcNow,
                OrderNumber = 1,
                IsFull = true
            };

            try
            {
                await _videoService.Upload(uploadVideoRequest);

                await _videoService.Upload(uploadThumbnailRequest);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to upload video", ex);
            }
        }

    }
}
