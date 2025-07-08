using BarClip.Models.Requests;
using Microsoft.AspNetCore.Http;

namespace BarClip.Core.Services
{
    public interface IVideoService
    {
        //Task<string> ReTrimVideo(ManualTrimRequest request);
        //Task<string> TrimVideo(IFormFile originalVideoFormFile);
        Task<SaveVideosRequest> TrimVideoFromStorage(string messageText);
    }
}