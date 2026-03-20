using Foundation;
using Photos;
using PhotosUI;

public class VideoPickerService
{
    public Task<List<FileResult>> PickVideosAsync()
    {
        var tcs = new TaskCompletionSource<List<FileResult>>();

        var config = new PHPickerConfiguration(PHPhotoLibrary.SharedPhotoLibrary)
        {
            Filter = PHPickerFilter.VideosFilter,
            SelectionLimit = 0 // 0 = unlimited
        };

        var picker = new PHPickerViewController(config);
        picker.Delegate = new PickerDelegate(tcs);

        var vc = Platform.GetCurrentUIViewController();
        vc?.PresentViewController(picker, true, null);

        return tcs.Task;
    }

    private class PickerDelegate : PHPickerViewControllerDelegate
    {
        private readonly TaskCompletionSource<List<FileResult>> _tcs;

        public PickerDelegate(TaskCompletionSource<List<FileResult>> tcs)
        {
            _tcs = tcs;
        }

        public override void DidFinishPicking(PHPickerViewController picker, PHPickerResult[] results)
        {
            picker.DismissViewController(true, null);

            if (results == null || results.Length == 0)
            {
                _tcs.SetResult(new List<FileResult>());
                return;
            }

            var fileResults = new FileResult[results.Length];


            var remaining = results.Length;

            SentrySdk.AddBreadcrumb($"Starting copy of {results.Length} videos");

            for (int i = 0; i < results.Length; i++)
            {
                var index = i;
                var result = results[i];
                var assetIdentifier = result.AssetIdentifier;
                var fetchResult = PHAsset.FetchAssetsUsingLocalIdentifiers(new[] { assetIdentifier }, null);
                var asset = fetchResult.firstObject as PHAsset;
                if (asset == null)
                {
                    SentrySdk.AddBreadcrumb($"Asset not found for identifier: {assetIdentifier}");
                    if (Interlocked.Decrement(ref remaining) == 0)
                        _tcs.SetResult(fileResults.Where(f => f != null).ToList());
                    continue;
                }
                var resources = PHAssetResource.GetAssetResources(asset);
                var videoResource = resources.FirstOrDefault(r => r.ResourceType == PHAssetResourceType.Video);
                if (videoResource == null)
                {
                    SentrySdk.AddBreadcrumb($"No video resource found for asset: {assetIdentifier}");
                    if (Interlocked.Decrement(ref remaining) == 0)
                        _tcs.SetResult(fileResults.Where(f => f != null).ToList());
                    continue;
                }
                var tempUrl = NSUrl.FromFilename(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".MOV"));
                var options = new PHAssetResourceRequestOptions { NetworkAccessAllowed = true };
                PHAssetResourceManager.DefaultManager.WriteData(
                    videoResource,
                    tempUrl,
                    options,
                    (error) =>
                    {
                        if (error != null)
                            SentrySdk.AddBreadcrumb($"WriteData error: {error.LocalizedDescription}");
                        else
                        {
                            SentrySdk.AddBreadcrumb($"WriteData complete: {tempUrl.Path}");
                            fileResults[index] = new FileResult(tempUrl.Path);
                        }
                        if (Interlocked.Decrement(ref remaining) == 0)
                            _tcs.SetResult(fileResults.Where(f => f != null).ToList());
                    });
            }
        }
    }
}