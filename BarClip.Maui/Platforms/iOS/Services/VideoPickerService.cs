using AVFoundation;
using Foundation;
using Photos;
using PhotosUI;
using UIKit;


public record PickedVideo(string Path, DateTime CreatedTime);

public class OutOfSpaceException : IOException
{
    public OutOfSpaceException() : base("Not enough storage space to import this video.") { }
}

public class VideoPickerService
{
        public async Task<List<PickedVideo>> PickVideosAsync(
        Action? onSelectionConfirmed = null,
        IProgress<double>? progress = null)
    {
        var tcs = new TaskCompletionSource<List<PickedVideo>>();

        var config = new PHPickerConfiguration(PHPhotoLibrary.SharedPhotoLibrary)
        {
            Filter = PHPickerFilter.VideosFilter,
            SelectionLimit = 0,
        };

        var picker = new PHPickerViewController(config);
        picker.Delegate = new PickerDelegate(tcs, onSelectionConfirmed, progress);

        var vc = Platform.GetCurrentUIViewController();
        vc?.PresentViewController(picker, true, null);

        return await tcs.Task;
    }


    public async Task<FileResult?> CaptureVideoAsync()
    {
        var authStatus = AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Video);

        if (authStatus == AVAuthorizationStatus.NotDetermined)
        {
            var granted = await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video);
            if (!granted)
            {
                SentrySdk.AddBreadcrumb("Camera access denied on first request");
                return null;
            }
        }
        else if (authStatus == AVAuthorizationStatus.Denied || authStatus == AVAuthorizationStatus.Restricted)
        {
            await ShowCameraPermissionDeniedAlertAsync();
            return null;
        }

        var tcs = new TaskCompletionSource<FileResult?>();

        var cameraVC = new CameraViewController(tcs)
        {
            ModalPresentationStyle = UIModalPresentationStyle.FullScreen
        };

        var vc = Platform.GetCurrentUIViewController();
        vc?.PresentViewController(cameraVC, true, null);

        return await tcs.Task;
    }

    private async Task ShowCameraPermissionDeniedAlertAsync()
    {
        var tcs = new TaskCompletionSource<bool>();

        var alert = UIAlertController.Create(
            "Camera Access Needed",
            "BarClip needs camera access to record videos. Please enable it in Settings.",
            UIAlertControllerStyle.Alert);

        alert.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, _ => tcs.SetResult(false)));
        alert.AddAction(UIAlertAction.Create("Open Settings", UIAlertActionStyle.Default, _ =>
        {
            var settingsUrl = new NSUrl(UIApplication.OpenSettingsUrlString);
            if (UIApplication.SharedApplication.CanOpenUrl(settingsUrl))
            {
                UIApplication.SharedApplication.OpenUrl(settingsUrl, new UIApplicationOpenUrlOptions(), null);
            }
            tcs.SetResult(true);
        }));

        var vc = Platform.GetCurrentUIViewController();
        vc?.PresentViewController(alert, true, null);

        await tcs.Task;
    }

    private class CameraDelegate : UIImagePickerControllerDelegate
    {
        private readonly TaskCompletionSource<FileResult?> _tcs;

        public CameraDelegate(TaskCompletionSource<FileResult?> tcs) => _tcs = tcs;

        public override async void FinishedPickingMedia(UIImagePickerController picker, NSDictionary info)
        {
            picker.DismissViewController(true, null);

            var mediaUrl = info[UIImagePickerController.MediaURL] as NSUrl;
            if (mediaUrl == null)
            {
                _tcs.SetResult(null);
                return;
            }

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".MOV");

            try
            {
                File.Copy(mediaUrl.Path!, tempPath);
                _tcs.SetResult(new FileResult(tempPath));
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _tcs.SetResult(null);
            }
        }

        public override void Canceled(UIImagePickerController picker)
        {
            picker.DismissViewController(true, null);
            _tcs.SetResult(null);
        }
    }

    private class PickerDelegate : PHPickerViewControllerDelegate
    {
        private readonly TaskCompletionSource<List<PickedVideo>> _tcs;
        private readonly Action? _onSelectionConfirmed;
        private readonly IProgress<double>? _progress;

        public PickerDelegate(
            TaskCompletionSource<List<PickedVideo>> tcs,
            Action? onSelectionConfirmed,
            IProgress<double>? progress)
        {
            _tcs = tcs;
            _onSelectionConfirmed = onSelectionConfirmed;
            _progress = progress;
        }

        public override async void DidFinishPicking(PHPickerViewController picker, PHPickerResult[] results)
        {
            picker.DismissViewController(true, null);

            // User cancelled the picker: not an error, just nothing to do.
            if (results == null || results.Length == 0)
            {
                _tcs.SetResult(new List<PickedVideo>());
                return;
            }

            var written = new List<PickedVideo>();

            try
            {
                // Let the caller show the loading screen before any (possibly slow) iCloud download starts.
                _onSelectionConfirmed?.Invoke();

                var identifiers = results.Select(r => r.AssetIdentifier).ToArray();
                var fetchResult = PHAsset.FetchAssetsUsingLocalIdentifiers(identifiers, null);
                var assets = Enumerable.Range(0, (int)fetchResult.Count)
                    .Select(i => fetchResult.ObjectAt(i) as PHAsset)
                    .Where(a => a != null)
                    .OrderBy(a => (DateTime)a!.CreationDate)
                    .ToList();

                SentrySdk.AddBreadcrumb($"Starting copy of {assets.Count} videos");

                for (int i = 0; i < assets.Count; i++)
                {
                    var asset = assets[i]!;
                    var resources = PHAssetResource.GetAssetResources(asset);
                    var videoResource = resources.FirstOrDefault(r => r.ResourceType == PHAssetResourceType.Video);

                    if (videoResource == null)
                    {
                        SentrySdk.AddBreadcrumb($"No video resource found for asset: {asset.LocalIdentifier}");
                        continue;
                    }

                    var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".MOV");
                    int index = i;
                    int total = assets.Count;

                    await WriteResourceAsync(
                        videoResource,
                        tempPath,
                        fileProgress => _progress?.Report((index + fileProgress) / total));

                    SentrySdk.AddBreadcrumb($"WriteData complete: {tempPath}");
                    written.Add(new PickedVideo(tempPath, (DateTime)asset.CreationDate));
                }

                _tcs.SetResult(written);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                SentrySdk.AddBreadcrumb($"Picker import failed: {ex.Message}");

                // Don't leave already-written temp files behind when the batch fails.
                foreach (var video in written)
                    TryDelete(video.Path);

                // Surface the failure so the caller can tell it apart from a cancel.
                _tcs.SetException(ex);
            }
        }

        private static Task WriteResourceAsync(PHAssetResource resource, string path, Action<double> onProgress)
        {
            var tcs = new TaskCompletionSource();

            var options = new PHAssetResourceRequestOptions
            {
                NetworkAccessAllowed = true,
                ProgressHandler = p => onProgress(p)
            };

            PHAssetResourceManager.DefaultManager.WriteData(resource, NSUrl.FromFilename(path), options, error =>
            {
                if (error == null)
                {
                    tcs.SetResult();
                    return;
                }

                // Remove any partially written file.
                TryDelete(path);

                // 640 = NSFileWriteOutOfSpaceError (Cocoa), 28 = ENOSPC (POSIX)
                bool outOfSpace =
                    (error.Domain == "NSCocoaErrorDomain" && error.Code == 640) ||
                    (error.Domain == "NSPOSIXErrorDomain" && error.Code == 28);

                tcs.SetException(outOfSpace
                    ? new OutOfSpaceException()
                    : new IOException(error.LocalizedDescription));
            });

            return tcs.Task;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Best effort cleanup.
            }
        }
    }


    //public async Task<FileResult?> CaptureVideoAsync()
    //{
    //    var authStatus = AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Video);

    //    if (authStatus == AVAuthorizationStatus.NotDetermined)
    //    {
    //        var granted = await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video);
    //        if (!granted)
    //        {
    //            SentrySdk.AddBreadcrumb("Camera access denied on first request");
    //            return null;
    //        }
    //    }
    //    else if (authStatus == AVAuthorizationStatus.Denied || authStatus == AVAuthorizationStatus.Restricted)
    //    {
    //        await ShowCameraPermissionDeniedAlertAsync();
    //        return null;
    //    }

    //    var tcs = new TaskCompletionSource<FileResult?>();

    //    var picker = new UIImagePickerController
    //    {
    //        SourceType = UIImagePickerControllerSourceType.Camera,
    //        MediaTypes = new string[] { "public.movie" },
    //        VideoQuality = 0,
    //        VideoMaximumDuration = 90
    //    };

    //    picker.Delegate = new CameraDelegate(tcs);

    //    var vc = Platform.GetCurrentUIViewController();
    //    vc?.PresentViewController(picker, true, null);

    //    return await tcs.Task;
    //}

}