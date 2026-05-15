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
            var videoRequest = new SasUrlRequest()
            {
                Id = sessionId,
                ContainerName = "videos",
                Extension = ".mov"
            };
            var thumnailRequest = new SasUrlRequest()
            {
                Id = sessionId,
                ContainerName = "thumbnails",
                Extension = ".jpg"
            };

            var sasUrlResponse = await _client.GetUploadSasUrlAsync(videoRequest);

            var thumbnailSasUrlResponse = await _client.GetUploadSasUrlAsync(thumnailRequest);

            var uploadVideoRequest = new UploadRequest()
            {
                Content = File.OpenRead(sessionPath),
                ContentType = "video/quicktime",
                UserId = sasUrlResponse.UserId,
                SasUrl = sasUrlResponse.UploadSasUrl,
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
                UserId = thumbnailSasUrlResponse.UserId,
                SasUrl = thumbnailSasUrlResponse.UploadSasUrl,
                VideoId = sessionId,
                SessionId = sessionId,
                CreatedAt = DateTime.UtcNow,
                OrderNumber = 1,
                IsFull = true
            };

            try
            {
                await _videoService.Upload(uploadVideoRequest);

            }
            catch (Exception ex)
            {
                throw new Exception("Failed to upload video", ex);
            }
            try
            {
                await _videoService.Upload(uploadThumbnailRequest);

            }
            catch (Exception ex)
            {
                throw new Exception("Failed to upload thumbnail", ex);
            }
        }
    }
}
