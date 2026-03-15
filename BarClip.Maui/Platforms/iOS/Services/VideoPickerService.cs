using MobileCoreServices;
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

            var fileResults = new List<FileResult>();
            var remaining = results.Length;

            foreach (var result in results)
            {
                result.ItemProvider.LoadFileRepresentation(
                "public.movie",
                (url, error) =>
                {
                    if (error != null)
                        SentrySdk.AddBreadcrumb($"LoadFileRepresentation error: {error.LocalizedDescription}");

                    if (url?.Path != null)
                    {
                        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(url.Path));
                        File.Copy(url.Path, tempPath, true);
                        fileResults.Add(new FileResult(tempPath));
                    }

                    if (Interlocked.Decrement(ref remaining) == 0)
                        _tcs.SetResult(fileResults);
                });
            }
        }
    }
}