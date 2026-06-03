using AVFoundation;
using Azure.Storage.Blobs.Models;
using BarClip.Data.Schema;
using BarClip.Models.Requests;
using CoreGraphics;
using CoreMedia;
using Foundation;
using GameController;
using HomeKit;
using ImageIO;
using MediaPlayer;
using Photos;
using UIKit;
using static BarClip.Core.Helpers.FileHelper;

namespace BarClip.Maui.Platforms.iOS.Helpers;

public class AVFoundationHelper
{

    public static async Task CompressVideoAsync(string inputPath, string outputPath, IProgress<double> progress = null)
    {
        var asset = AVUrlAsset.Create(NSUrl.FromFilename(inputPath));
        var exportSession = new AVAssetExportSession(asset, AVAssetExportSessionPreset.Preset640x480)
        {
            OutputUrl = NSUrl.FromFilename(outputPath),
            OutputFileType = "com.apple.quicktime-movie"
        };

        var exportTask = exportSession.ExportTaskAsync();

        var pollingTask = Task.Run(async () =>
        {
            while (!exportTask.IsCompleted)
            {
                progress?.Report(exportSession.Progress);
                await Task.Delay(200);
            }
        });

        if (await Task.WhenAny(exportTask, Task.Delay(TimeSpan.FromSeconds(60))) != exportTask)
        {
            SentrySdk.CaptureMessage($"Export timed out: {inputPath}");
            throw new Exception($"Compression timed out: {inputPath}");
        }

        await exportTask;
        progress?.Report(1.0);
        if (exportSession.Status != AVAssetExportSessionStatus.Completed)
        {
            SentrySdk.CaptureMessage($"Export failed. status={exportSession.Status}, error={exportSession.Error?.LocalizedDescription}, code={exportSession.Error?.Code}, domain={exportSession.Error?.Domain}");
            throw new Exception($"Compression failed: {exportSession.Error?.LocalizedDescription}");
        }

    }

    public static async Task ExtractSingleFrameAsync(string originalFilePath, double thumbnailTime, string thumbnailPath)
    {
        await Task.Run(() =>
        {
            using var asset = AVAsset.FromUrl(NSUrl.FromFilename(originalFilePath));

            using var imageGenerator = new AVAssetImageGenerator(asset)
            {
                AppliesPreferredTrackTransform = true,
                RequestedTimeToleranceBefore = CMTime.Zero,
                RequestedTimeToleranceAfter = CMTime.Zero
            };

            var time = CMTime.FromSeconds(thumbnailTime, 600);

            using var cgImage = imageGenerator.CopyCGImageAtTime(time, out _, out NSError error);

            if (error != null)
                throw new Exception($"Error extracting thumbnail: {error.LocalizedDescription}");

            if (cgImage == null)
                throw new Exception("Thumbnail generation returned null image.");

            using var destination = CGImageDestination.Create(NSUrl.FromFilename(thumbnailPath), "public.png", 1);

            if (destination == null)
                throw new Exception("Failed to create image destination.");

            destination.AddImage(cgImage);
            destination.Close();
        });
    }

    public static async Task ExtractAllFramesAsync(OriginalVideoRequest originalVideo, string tempFramePath, IProgress<double> progress = null)
    {
        double weight = .3;
        await Task.Run(async () =>
        {
            Directory.CreateDirectory(tempFramePath);

            using var asset = AVAsset.FromUrl(NSUrl.FromFilename(originalVideo.CompressedPath));

            using var imageGenerator = new AVAssetImageGenerator(asset)
            {
                AppliesPreferredTrackTransform = true,
            };

            var durationSeconds = originalVideo.Duration.TotalSeconds;

            int frameCount = (int)(durationSeconds * 1.0);

            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                using var pool = new NSAutoreleasePool();

                var time = CMTime.FromSeconds(frameIndex, 600);

                using var cgImage = imageGenerator.CopyCGImageAtTime(time, out _, out NSError error);

                if (error != null)
                    throw new Exception($"Error extracting frame: {error.LocalizedDescription}");

                if (cgImage == null)
                    continue; // skip invalid frame instead of crashing

                string filePath = Path.Combine(tempFramePath, $"frame_{frameIndex + 1}.png");

                using var destination = CGImageDestination.Create(NSUrl.FromFilename(filePath), "public.png", 1);

                if (destination == null)
                    throw new Exception("Failed to create image destination.");

                destination.AddImage(cgImage);
                destination.Close();

                progress?.Report((double)(frameIndex + 1) / frameCount * weight);

            }

        });
    }

    public async static Task<string[]> ExtractThumbnails(string originalFolderPath, string thumbnailFolderPath)
    {
        var originalFilePaths = Directory.GetFiles(originalFolderPath, "*.MOV");

        foreach (string originalFilePath in originalFilePaths)
        {
            string fileName = Path.GetFileNameWithoutExtension(originalFilePath);
            string thumbnailPath = Path.Combine(thumbnailFolderPath, fileName + ".png");

            using var asset = AVAsset.FromUrl(NSUrl.FromFilename(originalFilePath));

            // Ensure duration is loaded
            await asset.LoadValuesTaskAsync(new string[] { "duration" });

            var thumbnailTime = asset.Duration.Seconds / 2;

            try
            {
                await AVFoundationHelper.ExtractSingleFrameAsync(originalFilePath, thumbnailTime, thumbnailPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error extracting thumbnail from {originalFilePath}: {ex.Message}");
            }
        }

        return originalFilePaths;
    }
    public async static Task<string> ExtractThumbnail(string folderPath, string thumbnailFolderPath)
    {
        var filePaths = Directory.GetFiles(folderPath, "*.MOV");
        var filePath = filePaths.FirstOrDefault();
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        string thumbnailPath = Path.Combine(thumbnailFolderPath, fileName + ".png");

        using var asset = AVAsset.FromUrl(NSUrl.FromFilename(filePath));

        // Ensure duration is loaded
        await asset.LoadValuesTaskAsync(new string[] { "duration" });

        var thumbnailTime = asset.Duration.Seconds / 2;

        try
        {
            await AVFoundationHelper.ExtractSingleFrameAsync(filePath, thumbnailTime, thumbnailPath);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error extracting thumbnail from {filePath}: {ex.Message}");
        }

        return thumbnailPath;
    }



    public static async Task<string> MergeVideos(SessionFolderPaths sessionFolderPaths, Guid sessionId, IEnumerable<string> videoPaths, IProgress<double> progress = null)
    {
        var finalOutputPath = Path.Combine(sessionFolderPaths.Session, $"{sessionId}.MOV");
        if (File.Exists(finalOutputPath))
            File.Delete(finalOutputPath);

        var videoAssets = new List<AVUrlAsset>();
        foreach (string path in videoPaths)
        {
            NSUrl nsUrl = NSUrl.FromFilename(path);
            AVUrlAsset asset = AVUrlAsset.Create(nsUrl);
            videoAssets.Add(asset);
        }
        var composition = new AVMutableComposition();
        var compositionVideoTrack = composition.AddMutableTrack("vide", 1);
        var compositionAudioTrack = composition.AddMutableTrack("soun", 1);
        var insertTime = CMTime.Zero;
        int totalAssets = videoAssets.Count;
        int assetsLoaded = 0;

        foreach (var asset in videoAssets)
        {
            await asset.LoadValuesTaskAsync(["tracks"]);
            var assetVideoTrack = asset.GetTracks(AVMediaTypes.Video).FirstOrDefault();
            var assetAudioTrack = asset.GetTracks(AVMediaTypes.Audio).FirstOrDefault();
            var duration = asset.Duration;
            var timeRange = new CMTimeRange
            {
                Start = CMTime.Zero,
                Duration = duration
            };
            bool videoSuccess = compositionVideoTrack.InsertTimeRange(timeRange, assetVideoTrack, insertTime, out NSError videoError);
            bool audioSuccess = compositionAudioTrack.InsertTimeRange(timeRange, assetAudioTrack, insertTime, out NSError audioError);
            if (!videoSuccess || !audioSuccess)
                throw new Exception("");

            insertTime = CMTime.Add(insertTime, duration);

            assetsLoaded++;
        }

        // Orientation fix
        var firstVideoTrack = videoAssets[0].GetTracks(AVMediaTypes.Video).FirstOrDefault();
        var transform = firstVideoTrack.PreferredTransform;
        var naturalSize = firstVideoTrack.NaturalSize;
        var layerInstruction = AVMutableVideoCompositionLayerInstruction.FromAssetTrack(compositionVideoTrack);
        layerInstruction.SetTransform(transform, CMTime.Zero);
        var instruction = AVMutableVideoCompositionInstruction.Create() as AVMutableVideoCompositionInstruction;
        instruction.TimeRange = new CMTimeRange { Start = CMTime.Zero, Duration = composition.Duration };
        instruction.LayerInstructions = new[] { layerInstruction };
        var videoComposition = AVMutableVideoComposition.Create();
        var isPortrait = transform.B == 1 || transform.B == -1;
        videoComposition.RenderSize = isPortrait
            ? new CGSize(naturalSize.Height, naturalSize.Width)
            : new CGSize(naturalSize.Width, naturalSize.Height);
        videoComposition.FrameDuration = new CMTime(1, 60);
        videoComposition.Instructions = new[] { instruction };

        var exportSession = new AVAssetExportSession(composition, AVAssetExportSessionPreset.HighestQuality)
        {
            OutputUrl = NSUrl.FromFilename(finalOutputPath),
            OutputFileType = "com.apple.quicktime-movie",
            ShouldOptimizeForNetworkUse = true,
            VideoComposition = videoComposition
        };

        var exportTask = exportSession.ExportTaskAsync();
        while (!exportTask.IsCompleted)
        {
            progress?.Report(0.9 + exportSession.Progress * 0.1);
            await Task.Delay(200);
        }
        await exportTask;
        progress?.Report(1.0);

        if (exportSession.Status != AVAssetExportSessionStatus.Completed)
        {
            var details = new System.Text.StringBuilder();
            details.AppendLine($"Status: {exportSession.Status}");
            if (exportSession.Error != null)
            {
                details.AppendLine($"Description: {exportSession.Error.LocalizedDescription}");
                details.AppendLine($"Domain: {exportSession.Error.Domain}");
                details.AppendLine($"Code: {exportSession.Error.Code}");
                details.AppendLine($"User Info: {exportSession.Error.UserInfo}");
            }
            throw new Exception($"Merge failed — {details}");
        }

        return finalOutputPath;
    }

    public static async Task TrimAsync(OriginalVideoRequest originalVideo, ProcessedVideoRequest processedVideo, IProgress<double> progress = null)
    {
        await Task.Run(async () =>
        {
            using var asset = AVAsset.FromUrl(NSUrl.FromFilename(originalVideo.FilePath));

            try
            {
                await asset.LoadValuesTaskAsync(new[] { "tracks", "duration" });
            }
            catch (Exception ex)
            {
                throw new Exception("Error loading video asset", ex);
            }

            if (File.Exists(processedVideo.FilePath))
                File.Delete(processedVideo.FilePath);

            var startTime = CMTime.FromSeconds(originalVideo.TrimStart.TotalSeconds, 600);
            var duration = CMTime.FromSeconds(processedVideo.Duration.TotalSeconds, 600);

            using var exportSession = new AVAssetExportSession(asset, AVAssetExportSessionPreset.HighestQuality)
            {
                OutputUrl = NSUrl.FromFilename(processedVideo.FilePath),
                OutputFileType = "com.apple.quicktime-movie",
                ShouldOptimizeForNetworkUse = true,
                TimeRange = new CMTimeRange { Start = startTime, Duration = duration }
            };

            var exportTask = exportSession.ExportTaskAsync();
            while (!exportTask.IsCompleted)
            {
                progress?.Report(0.8 + exportSession.Progress * 0.2);
                await Task.Delay(200);
            }
            await exportTask;
            progress?.Report(1.0);


            if (exportSession.Status != AVAssetExportSessionStatus.Completed)
            {
                var details = new System.Text.StringBuilder();
                details.AppendLine($"Status: {exportSession.Status}");
                if (exportSession.Error != null)
                {
                    details.AppendLine($"Description: {exportSession.Error.LocalizedDescription}");
                    details.AppendLine($"Domain: {exportSession.Error.Domain}");
                    details.AppendLine($"Code: {exportSession.Error.Code}");
                    details.AppendLine($"User Info: {exportSession.Error.UserInfo}");
                }
                throw new Exception($"Trim failed — {details}");
            }
        });
    }
    public static async Task<TimeSpan> GetDurationAsync(string filePath)
    {
        using var asset = AVAsset.FromUrl(NSUrl.FromFilename(filePath));
        await asset.LoadValuesTaskAsync(new string[] { "duration" });
        return TimeSpan.FromSeconds(asset.Duration.Seconds);
    }
    public static async Task SaveVideoToCameraRoll(NSUrl videoUrl)
    {
        var status = await PHPhotoLibrary.RequestAuthorizationAsync(PHAccessLevel.AddOnly);

        if (status != PHAuthorizationStatus.Authorized)
            throw new Exception("Photo library access denied");

        var tcs = new TaskCompletionSource<bool>();

        PHPhotoLibrary.SharedPhotoLibrary.PerformChanges(() =>
        {
            PHAssetChangeRequest.FromVideo(videoUrl);
        }, (success, error) =>
        {
            if (error != null)
                tcs.SetException(new Exception(error.LocalizedDescription));
            else
                tcs.SetResult(success);
        });

        await tcs.Task;
    }

}