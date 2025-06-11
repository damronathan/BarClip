using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace BarClip.Core.Services;

public class StorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private const string OriginalVideosContainer = "originalvideos";

    public StorageService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<string> DownloadVideoAsync(Guid blobName)
    {
        string tempFilePath = Path.GetTempPath();

        var containerClient = _blobServiceClient.GetBlobContainerClient(OriginalVideosContainer);

        var blobClient = containerClient.GetBlobClient(blobName.ToString() + ".mp4");

        string videoFilePath = Path.Combine(tempFilePath, blobName.ToString() + ".mp4");

        await blobClient.DownloadToAsync(videoFilePath);

        return videoFilePath;
    }

    public async Task UploadVideoAsync(Guid blobName, string filePath, string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        var blobClient = containerClient.GetBlobClient(blobName.ToString() + ".mp4");

        await blobClient.UploadAsync(filePath, overwrite: true);
    }

    public string GenerateSasUrl(Guid blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("trimmedvideos");

        var blobClient = containerClient.GetBlobClient(blobName.ToString() + ".mp4");

        var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));

        return sasUri.ToString();
    }

    public async Task DeleteVideoAsync(Guid blobName, string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        var blobClient = containerClient.GetBlobClient(blobName.ToString() + ".mp4");

        Console.WriteLine($"{blobName.ToString()}.mp4");

        var response = await blobClient.DeleteIfExistsAsync();
    }
}
