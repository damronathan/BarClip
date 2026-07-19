using AVFoundation;
using Foundation;
using Photos;
using PhotosUI;
using UIKit;

public class VideoPickerService
{
    public async Task<List<FileResult>> PickVideosAsync()
    {
        var tcs = new TaskCompletionSource<List<FileResult>>();

        var config = new PHPickerConfiguration(PHPhotoLibrary.SharedPhotoLibrary)
        {
            Filter = PHPickerFilter.VideosFilter,
            SelectionLimit = 0,
        };

        var picker = new PHPickerViewController(config);
        picker.Delegate = new PickerDelegate(tcs);

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

        var picker = new UIImagePickerController
        {
            SourceType = UIImagePickerControllerSourceType.Camera,
            MediaTypes = new string[] { "public.movie" },
            VideoQuality = UIImagePickerControllerQualityType.High,
            VideoMaximumDuration = 90
        };

        picker.Delegate = new CameraDelegate(tcs);

        var vc = Platform.GetCurrentUIViewController();
        vc?.PresentViewController(picker, true, null);

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
        private readonly TaskCompletionSource<List<FileResult>> _tcs;

        public PickerDelegate(TaskCompletionSource<List<FileResult>> tcs) => _tcs = tcs;

        public override async void DidFinishPicking(PHPickerViewController picker, PHPickerResult[] results)
        {
            picker.DismissViewController(true, null);

            if (results == null || results.Length == 0)
            {
                _tcs.SetResult(new List<FileResult>());
                return;
            }

            var identifiers = results.Select(r => r.AssetIdentifier).ToArray();
            var fetchResult = PHAsset.FetchAssetsUsingLocalIdentifiers(identifiers, null);
            var assets = Enumerable.Range(0, (int)fetchResult.Count)
                .Select(i => fetchResult.ObjectAt(i) as PHAsset)
                .Where(a => a != null)
                .OrderBy(a => (DateTime)a!.CreationDate)
                .ToList();

            SentrySdk.AddBreadcrumb($"Starting copy of {assets.Count} videos");

            var fileResults = new List<FileResult>();

            foreach (var asset in assets)
            {
                var resources = PHAssetResource.GetAssetResources(asset!);
                var videoResource = resources.FirstOrDefault(r => r.ResourceType == PHAssetResourceType.Video);

                if (videoResource == null)
                {
                    SentrySdk.AddBreadcrumb($"No video resource found for asset: {asset!.LocalIdentifier}");
                    continue;
                }

                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".MOV");
                var tempUrl = NSUrl.FromFilename(tempPath);
                var options = new PHAssetResourceRequestOptions { NetworkAccessAllowed = true };

                try
                {
                    await PHAssetResourceManager.DefaultManager.WriteDataAsync(videoResource, tempUrl, options);
                    SentrySdk.AddBreadcrumb($"WriteData complete: {tempPath}");
                    fileResults.Add(new FileResult(tempPath));
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                    SentrySdk.AddBreadcrumb($"WriteData failed for asset {asset!.LocalIdentifier}: {ex.Message}");
                }
            }

            _tcs.SetResult(fileResults);
        }
    }
}