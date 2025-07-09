using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace BarClip.Core.Services;

public class StorageService
{
    private readonly BlobServiceClient _blobServiceClient;

    public StorageService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<(string, string)> DownloadVideoAsync(string fileName, string containerName)
    {
        string tempFilePath = Path.GetTempPath();

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        var blobClient = containerClient.GetBlobClient(fileName);

        string videoFilePath = Path.Combine(tempFilePath, fileName);

        await blobClient.DownloadToAsync(videoFilePath);

        var properties = await blobClient.GetPropertiesAsync();

        var userId = GetRequiredGuidFromMetadata(properties.Value.Metadata, "userId");

        return (videoFilePath, userId);
    }

    public async Task UploadVideoAsync(Guid blobName, string filePath, string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        var blobClient = containerClient.GetBlobClient(blobName.ToString() + ".mp4");

        await blobClient.UploadAsync(filePath, overwrite: true);
    }

    public async Task CopyVideoAsync(string sourceBlobName, Guid destinationBlobId, string containerName)
    {
        var sourceContainerClient = _blobServiceClient.GetBlobContainerClient("originalvideos");
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        var sourceBlobClient = sourceContainerClient.GetBlobClient(sourceBlobName);
        var destinationBlobClient = containerClient.GetBlobClient(destinationBlobId.ToString() + ".mp4");

        await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);

        await sourceBlobClient.DeleteIfExistsAsync();
    }

    public string GenerateUploadSasUrl(Guid blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("originalvideos");

        var blobClient = containerClient.GetBlobClient(blobName.ToString() + ".mov");

        var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Create, DateTimeOffset.UtcNow.AddHours(1));

        return sasUri.ToString();
    }

    public string GenerateDownloadSasUrl(Guid blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("trimmedvideos");

        var blobClient = containerClient.GetBlobClient(blobName.ToString() + ".mp4");

        var sasUri =  blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));

        return sasUri.ToString();
    }

    public async Task DeleteVideoAsync(Guid blobName, string containerName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        var blobClient = containerClient.GetBlobClient(blobName.ToString() + ".mp4");

        var response = await blobClient.DeleteIfExistsAsync();
    }
    private static string GetRequiredGuidFromMetadata(IDictionary<string, string> metadata, string key)
    {
        if (!metadata.TryGetValue(key, out var value))
        {
            throw new Exception("No userId");
        }
        return value;
            



    }
}
