using Foundation;
using Photos;
using PhotosUI;

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