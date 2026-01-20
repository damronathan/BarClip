using BarClip.Models.Domain;
using BarClip.Models.Requests;
using static BarClip.Core.Helpers.FileHelper;

namespace BarClip.Core.Interfaces;

public interface IVideoEditor
{
    Task<List<Frame>> ExtractAndProcessFrames(OriginalVideoRequest originalVideo);
    Task<string> MergeVideos(SessionFolderPaths sessionFolderPaths, Guid sessionId);
    Task<string[]> ExtractThumbnails(string originalFolderPath, string thumbnailFolderPath);
    Task<ProcessedVideoRequest> ProcessVideo(SessionFolderPaths sessionFolderPaths, OriginalVideoRequest video);
}
