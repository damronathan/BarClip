using Azure.Storage.Blobs;


namespace BarClip.Core.Services;

public class StorageService
{
    public static async Task<string> DownloadVideo(string blobName)
    {
        string tempFilePath = Path.GetTempPath();

        string connectionString = "DefaultEndpointsProtocol=https;AccountName=barclipstorage;AccountKey=D4vHYa+EaLCffbGHQrH2OcmFoUOfCCu/XYBgQKs+D9ugjEHUmTL94I5CQGfAqCUwaYRZDptGhng/+AStd8QXQg==;EndpointSuffix=core.windows.net";
        var blobServiceClient = new BlobServiceClient(connectionString);

        string containerName = "originalvideos";
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        var blobClient = containerClient.GetBlobClient(blobName);

        string videoFilePath = Path.Combine(tempFilePath, blobName);

        await blobClient.DownloadToAsync(videoFilePath);

        return videoFilePath;
    }
    public static async Task UploadVideo(string blobName, string filePath)
    {
        string connectionString = "DefaultEndpointsProtocol=https;AccountName=barclipstorage;AccountKey=D4vHYa+EaLCffbGHQrH2OcmFoUOfCCu/XYBgQKs+D9ugjEHUmTL94I5CQGfAqCUwaYRZDptGhng/+AStd8QXQg==;EndpointSuffix=core.windows.net";
        var blobServiceClient = new BlobServiceClient(connectionString);

        string containerName = "trimmedvideos";

        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(filePath, overwrite: true);

        
    }
}
