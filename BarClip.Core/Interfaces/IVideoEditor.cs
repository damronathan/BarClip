using BarClip.Models.Domain;
using BarClip.Models.Requests;
using static BarClip.Core.Helpers.FileHelper;

namespace BarClip.Core.Interfaces;

public interface IVideoEditor
{
    Task<List<Frame>> ExtractAndProcessFrames(OriginalVideoRequest originalVideo, IProgress<double> progress = null);
    Task<string> MergeVideos(SessionFolderPaths sessionFolderPaths, Guid sessionId, IEnumerable<string> videoPaths, IProgress<double> progress = null);
    Task<string[]> ExtractThumbnails(string originalFolderPath, string thumbnailFolderPath);
    Task<ProcessedVideoRequest> ProcessVideo(SessionFolderPaths sessionFolderPaths, OriginalVideoRequest video, IProgress<double> progress = null);
    Task CompressVideo(string inputPath, string outputPath, IProgress<double> progress = null);
    Task<string> ExtractThumbnail(string folderPath, string thumbnailFolderPath);
    Task SaveVideo(string videoPath);
}
