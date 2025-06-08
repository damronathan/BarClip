using Xabe.FFmpeg;
using BarClip.Data.Schema;

namespace BarClip.Core.Services;

public class VideoProcessorService
{
    public static async Task TrimOriginalVideo(string originalVideoBlob)
    {
        

        var tempVideoPath = await StorageService.DownloadVideo(originalVideoBlob);

        var originalVideo = new Video()
        {
            Name = originalVideoBlob,
            FilePath = tempVideoPath,
            VideoInfo = await FFmpeg.GetMediaInfo(tempVideoPath)
        };

        var tempFramePath = await ExtractFrames(originalVideo);

        originalVideo.Frames = DetectionService.DetectPlates(tempFramePath);

        var trimmedVideo = await TrimService.Trim(originalVideo);

        await StorageService.UploadVideo(trimmedVideo.Name, trimmedVideo.FilePath);
    }

    //Extracts frames from original video and saves them to temp frame path.
    private static async Task<string> ExtractFrames(Video video)
    {
        IVideoStream videoStream = video.VideoInfo.VideoStreams.First().SetCodec(VideoCodec.png);

        string tempFramePath = Path.Combine(Path.GetTempPath(), "frames");

        string outputFileNameBuilder(string number) => Path.Combine(tempFramePath, $"frame_{number}.png");

        int fps = (int)Math.Round(videoStream.Framerate);

        try
        {
            await FFmpeg.Conversions.New()
                .AddStream(videoStream)
                .ExtractEveryNthFrame(fps, outputFileNameBuilder)
                .Start();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error extracting frames: {ex.Message}");
        }
        return tempFramePath;
    }

}
