using BarClip.Core.Repositories;
using BarClip.Models.Requests;
using FFMpegCore;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarClip.Core.Services;
public interface IVideoDataService
{
    string GetDownloadSasUrl(Guid trimmedVideoId);
    string GetUploadSasUrl(Guid blobName);
    Task SaveVideos(SaveVideosRequest request);
}
public class VideoDataService : IVideoDataService
{
    private readonly StorageService _storageService;
    private readonly VideoRepository _repo;

    public VideoDataService(StorageService storageService, VideoRepository repo)
    {
        _storageService = storageService;
        _repo = repo;
    }

    public async Task SaveVideos(SaveVideosRequest request)
    {
        var url = _storageService.GenerateDownloadSasUrl(request.TrimmedVideo.Id);

        if (string.IsNullOrEmpty(url))
        {
            throw new InvalidOperationException("Failed to generate download SAS URL");
        }

        await _repo.SaveVideosAsync(request);
    }

    public string GetDownloadSasUrl(Guid trimmedVideoId) => _storageService.GenerateDownloadSasUrl(trimmedVideoId);
    public string GetUploadSasUrl(Guid blobName) => _storageService.GenerateUploadSasUrl(blobName);
}