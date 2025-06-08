using BarClip.Data.Schema;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Azure.Storage.Blobs;


namespace BarClip.Core.Services;

public static class StorageService
{
    public static async Task<(string, string)> DownloadOriginalVideo(string tempFilePath)
    {
        string connectionString = "DefaultEndpointsProtocol=https;AccountName=barclipstorage;AccountKey=D4vHYa+EaLCffbGHQrH2OcmFoUOfCCu/XYBgQKs+D9ugjEHUmTL94I5CQGfAqCUwaYRZDptGhng/+AStd8QXQg==;EndpointSuffix=core.windows.net";
        var blobServiceClient = new BlobServiceClient(connectionString);

        string containerName = "originalvideos";
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        string blobName = "206cj.mp4";
        var blobClient = containerClient.GetBlobClient(blobName);

        string videoFilePath = Path.Combine(tempFilePath, blobName);

        await blobClient.DownloadToAsync(videoFilePath);

        return (videoFilePath, blobName);
    }
}
