using AVFoundation;
using BarClip.Models.Requests;
using CoreAnimation;
using CoreGraphics;
using CoreMedia;
using Foundation;
using ImageIO;
using MediaPlayer;
using Photos;
using UIKit;
using static BarClip.Core.Helpers.FileHelper;




namespace BarClip.Maui.Platforms.iOS.Helpers;

public class AVFoundationHelper
{
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

    public static async Task ExtractAllFramesAsync(OriginalVideoRequest originalVideo, string tempFramePath)
    {
        await Task.Run(async () =>
        {
            Directory.CreateDirectory(tempFramePath);

            using var asset = AVAsset.FromUrl(NSUrl.FromFilename(originalVideo.FilePath));

            // Ensure duration is loaded
            await asset.LoadValuesTaskAsync(new string[] { "duration" });

            var durationSeconds = asset.Duration.Seconds;

            using var imageGenerator = new AVAssetImageGenerator(asset)
            {
                AppliesPreferredTrackTransform = true,
                RequestedTimeToleranceBefore = CMTime.Zero,
                RequestedTimeToleranceAfter = CMTime.Zero
            };

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

   public static async Task<string> MergeVideos(string[] videoPaths, string outputPath)
{
    var composition = new AVMutableComposition();
    
    // Create one track for video and one for audio
    var videoTrack = composition.AddMutableTrack("com.apple.quicktime-movie", 0);
    var audioTrack = composition.AddMutableTrack("com.apple.m4a-audio", 0);
    var currentTime = CMTime.Zero;

    foreach (var path in videoPaths)
    {
        var asset = AVAsset.FromUrl(NSUrl.FromFilename(path));
        await asset.LoadValuesTaskAsync(new[] { "tracks", "duration" });

        var assetVideo = asset.GetTracks(AVMediaTypes.Video).FirstOrDefault();
        var assetAudio = asset.GetTracks(AVMediaTypes.Audio).FirstOrDefault();

        var timeRange = new CMTimeRange { Start = CMTime.Zero, Duration = asset.Duration };

        // Append to the end of the existing tracks
        videoTrack.InsertTimeRange(timeRange, assetVideo, currentTime, out _);
        if (assetAudio != null)
            audioTrack.InsertTimeRange(timeRange, assetAudio, currentTime, out _);

        currentTime = CMTime.Add(currentTime, asset.Duration);
    }

    // Set the overall orientation based on the first clip
    videoTrack.PreferredTransform = composition.TracksWithMediaType("com.apple.quicktime-movie")[0].PreferredTransform;

    if (File.Exists(outputPath)) File.Delete(outputPath);

    var exportSession = new AVAssetExportSession(composition, AVAssetExportSessionPreset.HighestQuality)
    {
        OutputUrl = NSUrl.FromFilename(outputPath),
        OutputFileType = "com.apple.quicktime-movie"
    };

    await exportSession.ExportTaskAsync();
    return outputPath;
}

    public static async Task TrimAndLabelAsync(OriginalVideoRequest originalVideo, ProcessedVideoRequest processedVideo, string? weightText)
    {
        await Task.Run(async () =>
        {
            using var asset = AVAsset.FromUrl(NSUrl.FromFilename(originalVideo.FilePath));

            try
            {
                await asset.LoadValuesTaskAsync(new[] { "tracks" });
            }
            catch (Exception ex)
            {
                throw new Exception("Error loading video tracks", ex);
            }

            var videoTracks = asset.GetTracks(AVMediaTypes.Video);
            var audioTracks = asset.GetTracks(AVMediaTypes.Audio);

            var sourceVideoTrack = videoTracks?.FirstOrDefault();
            var audioTrack = audioTracks?.FirstOrDefault();

            if (sourceVideoTrack == null)
                throw new Exception($"No video track found. Total tracks: {asset.Tracks?.Length ?? 0}");

            using var composition = new AVMutableComposition();

            try
            {
                var compositionVideoTrack = composition.AddMutableTrack(AVMediaTypes.Video.ToString(), 0);
                var compositionAudioTrack = composition.AddMutableTrack(AVMediaTypes.Audio.ToString(), 0);

                var startTime = CMTime.FromSeconds(originalVideo.TrimStart.TotalSeconds, 600);
                var duration = CMTime.FromSeconds(processedVideo.Duration.TotalSeconds, 600);
                var timeRange = new CMTimeRange { Start = startTime, Duration = duration };

                var assetAudioTrack = asset.Tracks
                    .FirstOrDefault(t => t.MediaType == AVMediaTypes.Audio.ToString());

                compositionVideoTrack?.InsertTimeRange(timeRange, sourceVideoTrack, CMTime.Zero, out _);

                if (assetAudioTrack != null && compositionAudioTrack != null)
                    compositionAudioTrack.InsertTimeRange(timeRange, assetAudioTrack, CMTime.Zero, out _);

                compositionVideoTrack.PreferredTransform = sourceVideoTrack.PreferredTransform;

                var videoComposition = AVMutableVideoComposition.Create();
                videoComposition.FrameDuration = new CMTime(1, 60);
                var naturalSize = sourceVideoTrack.NaturalSize;
                var transform = sourceVideoTrack.PreferredTransform;
                var isPortrait = transform.B == 1 || transform.B == -1;
                videoComposition.RenderSize = isPortrait
                    ? new CGSize(naturalSize.Height, naturalSize.Width)
                    : naturalSize;
                var instruction = AVMutableVideoCompositionInstruction.Create();
                instruction.TimeRange = new CMTimeRange { Start = CMTime.Zero, Duration = duration };

                var layerInstruction = AVMutableVideoCompositionLayerInstruction.FromAssetTrack(compositionVideoTrack);
                instruction.LayerInstructions = new[] { layerInstruction };

                videoComposition.Instructions = new[] { instruction };

                if (weightText is not null)
                {
                    var videoSize = sourceVideoTrack.NaturalSize;

                    var parentLayer = new CALayer
                    {
                        Frame = new CGRect(0, 0, videoSize.Width, videoSize.Height)
                    };

                    var videoLayer = new CALayer
                    {
                        Frame = parentLayer.Frame
                    };

                    var textLayer = new CATextLayer
                    {
                        String = weightText,
                        FontSize = 150,
                        ForegroundColor = UIColor.White.CGColor,
                        Frame = new CGRect(0, 350, videoSize.Width, 200),
                        BorderColor = UIColor.Black.CGColor,
                        BorderWidth = 2
                    };

                    parentLayer.AddSublayer(videoLayer);
                    parentLayer.AddSublayer(textLayer);

                    videoComposition.AnimationTool = AVVideoCompositionCoreAnimationTool.FromLayer(videoLayer, parentLayer);
                }

                if (File.Exists(processedVideo.FilePath))
                    File.Delete(processedVideo.FilePath);

                using var exportSession = new AVAssetExportSession(composition, AVAssetExportSessionPreset.HighestQuality)
                {
                    OutputUrl = NSUrl.FromFilename(processedVideo.FilePath),
                    OutputFileType = "com.apple.quicktime-movie",
                    VideoComposition = videoComposition,
                    ShouldOptimizeForNetworkUse = true
                };

                var waitHandle = new ManualResetEventSlim(false);
                NSError exportError = null;
                AVAssetExportSessionStatus exportStatus = AVAssetExportSessionStatus.Unknown;
                SentrySdk.AddBreadcrumb($"TrimStart: {originalVideo.TrimStart}, Duration: {processedVideo.Duration}");
                SentrySdk.AddBreadcrumb($"Asset duration: {asset.Duration.Seconds}s");
                SentrySdk.AddBreadcrumb($"Track count: {asset.Tracks?.Length ?? 0}");
                SentrySdk.AddBreadcrumb($"Source video track size: {sourceVideoTrack.NaturalSize.Width}x{sourceVideoTrack.NaturalSize.Height}");
                SentrySdk.AddBreadcrumb($"Composition video track count: {composition.Tracks?.Length ?? 0}");
                SentrySdk.AddBreadcrumb($"RenderSize: {videoComposition.RenderSize.Width}x{videoComposition.RenderSize.Height}");
                SentrySdk.AddBreadcrumb($"NaturalSize: {naturalSize.Width}x{naturalSize.Height}");
                SentrySdk.AddBreadcrumb($"Transform B: {transform.B}, isPortrait: {isPortrait}");
                exportSession.ExportAsynchronously(() =>
                {
                    exportError = exportSession.Error;
                    exportStatus = exportSession.Status;
                    waitHandle.Set();
                });

                waitHandle.Wait();

                if (exportStatus != AVAssetExportSessionStatus.Completed)
                {
                    var details = new System.Text.StringBuilder();
                    details.AppendLine($"Status: {exportStatus}");

                    if (exportError != null)
                    {
                        details.AppendLine($"Description: {exportError.LocalizedDescription}");
                        details.AppendLine($"Domain: {exportError.Domain}");
                        details.AppendLine($"Code: {exportError.Code}");
                        details.AppendLine($"User Info: {exportError.UserInfo}");
                    }

                    throw new Exception($"Export failed — {details} trim start: {originalVideo.TrimStart}, trim finish {originalVideo.TrimFinish}");
                }
            }
            catch (Exception ex) when (!ex.Message.StartsWith("Error processing video") && !ex.Message.StartsWith("No video track"))
            {
                throw new Exception("Error building and exporting composition", ex);
            }
        });
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
    public static async Task TrimAsync(OriginalVideoRequest originalVideo, ProcessedVideoRequest processedVideo)
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

            await exportSession.ExportTaskAsync();

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
        await SaveVideoToCameraRoll(NSUrl.FromFilename(processedVideo.FilePath));

    }
}