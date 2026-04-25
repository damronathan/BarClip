using BarClip.Models.Requests;
using System.Globalization;
using System.Net.Http.Headers;

namespace BarClip.Core.Services;

public class StorageService
{
    private readonly HttpClient _httpClient;

    public StorageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task UploadAsync(UploadVideoRequest request)
    {
        using var streamContent = new StreamContent(request.Content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(request.ContentType);

        var httpRequest = new HttpRequestMessage(HttpMethod.Put, request.SasUrl)
        {
            Content = streamContent
        };

        // Required for Azure Blob
        httpRequest.Headers.Add("x-ms-blob-type", "BlockBlob");

        // Metadata headers
        httpRequest.Headers.Add("x-ms-meta-userid", request.UserId);
        httpRequest.Headers.Add("x-ms-meta-videoid", request.VideoId.ToString());
        httpRequest.Headers.Add("x-ms-meta-sessionid", request.SessionId.ToString());
        httpRequest.Headers.Add("x-ms-meta-createdat",
            request.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        httpRequest.Headers.Add("x-ms-meta-ordernumber", request.OrderNumber.ToString());
        httpRequest.Headers.Add("x-ms-meta-isfull", request.IsFull.ToString());

        var response = await _httpClient.SendAsync(httpRequest);

        response.EnsureSuccessStatusCode();
    }
}
