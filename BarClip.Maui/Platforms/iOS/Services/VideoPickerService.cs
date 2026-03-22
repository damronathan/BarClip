using CoreGraphics;
using Foundation;
using ObjCRuntime;
using Photos;
using UIKit;

public class VideoPickerService
{
    public Task<List<FileResult>> PickVideosAsync()
    {
        var tcs = new TaskCompletionSource<List<FileResult>>();
        var picker = new OrderedVideoPickerController();
        picker.OnCompleted = (orderedAssets) => HandleAssets(orderedAssets, tcs);

        var vc = Platform.GetCurrentUIViewController();
        vc?.PresentViewController(picker, true, null);

        return tcs.Task;
    }

    private void HandleAssets(List<PHAsset> orderedAssets, TaskCompletionSource<List<FileResult>> tcs)
    {
        if (orderedAssets == null || orderedAssets.Count == 0)
        {
            tcs.SetResult(new List<FileResult>());
            return;
        }

        var fileResults = new List<FileResult>();
        var remaining = orderedAssets.Count;

        SentrySdk.AddBreadcrumb($"Starting copy of {orderedAssets.Count} videos");

        foreach (var asset in orderedAssets)
        {
            var resources = PHAssetResource.GetAssetResources(asset);
            var videoResource = resources.FirstOrDefault(r => r.ResourceType == PHAssetResourceType.Video);

            if (videoResource == null)
            {
                SentrySdk.AddBreadcrumb($"No video resource found for asset");
                if (Interlocked.Decrement(ref remaining) == 0)
                    tcs.SetResult(fileResults);
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
                        lock (fileResults)
                            fileResults.Add(new FileResult(tempUrl.Path));
                    }

                    if (Interlocked.Decrement(ref remaining) == 0)
                        tcs.SetResult(fileResults);
                });
        }
    }

    // -------------------------------------------------------
    // Custom picker UI
    // -------------------------------------------------------

    private class OrderedVideoPickerController : UIViewController
    {
        public Action<List<PHAsset>>? OnCompleted;

        private UICollectionView _collectionView = null!;
        private PHFetchResult _fetchResult = null!;
        private readonly List<PHAsset> _orderedSelection = new();

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View!.BackgroundColor = UIColor.SystemBackground;

            // Nav bar
            Title = "Select Videos";
            NavigationItem.LeftBarButtonItem = new UIBarButtonItem("Cancel", UIBarButtonItemStyle.Plain, (s, e) =>
            {
                DismissViewController(true, null);
                OnCompleted?.Invoke(new List<PHAsset>());
            });
            NavigationItem.RightBarButtonItem = new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, (s, e) =>
            {
                DismissViewController(true, null);
                OnCompleted?.Invoke(_orderedSelection);
            });

            // Collection view
            var layout = new UICollectionViewFlowLayout
            {
                MinimumInteritemSpacing = 2,
                MinimumLineSpacing = 2
            };
            var cellSize = (UIScreen.MainScreen.Bounds.Width - 6) / 3;
            layout.ItemSize = new CGSize(cellSize, cellSize);

            _collectionView = new UICollectionView(View.Bounds, layout)
            {
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
                AllowsMultipleSelection = false // we handle selection manually
            };
            _collectionView.RegisterClassForCell(typeof(VideoThumbnailCell), VideoThumbnailCell.ReuseId);
            _collectionView.DataSource = new VideoDataSource(this);
            _collectionView.Delegate = new VideoCollectionDelegate(this);

            View.AddSubview(_collectionView);

            // Fetch all videos, newest first
            var options = new PHFetchOptions();
            options.SortDescriptors = new[] { new NSSortDescriptor("creationDate", false) };
            _fetchResult = PHAsset.FetchAssets(PHAssetMediaType.Video, options);

            _collectionView.ReloadData();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            if (NavigationController == null)
            {
                // Wrap in a nav controller if presented directly
                DismissViewController(false, null);
                var nav = new UINavigationController(this);
                Platform.GetCurrentUIViewController()?.PresentViewController(nav, true, null);
            }
        }

        public PHFetchResult FetchResult => _fetchResult;
        public List<PHAsset> OrderedSelection => _orderedSelection;

        public void ToggleSelection(PHAsset asset, VideoThumbnailCell cell)
        {
            var existing = _orderedSelection.IndexOf(asset);
            if (existing >= 0)
            {
                // Deselect — remove and re-number all badges
                _orderedSelection.RemoveAt(existing);
                RefreshAllBadges();
            }
            else
            {
                _orderedSelection.Add(asset);
                cell.SetBadge(_orderedSelection.Count);
            }

            // Update done button title
            NavigationItem.RightBarButtonItem!.Title = _orderedSelection.Count > 0
                ? $"Done ({_orderedSelection.Count})"
                : "Done";
        }

        private void RefreshAllBadges()
        {
            foreach (var cell in _collectionView.VisibleCells.OfType<VideoThumbnailCell>())
            {
                var indexPath = _collectionView.IndexPathForCell(cell);
                if (indexPath == null) continue;
                var asset = _fetchResult.ObjectAt(indexPath.Row) as PHAsset;
                if (asset == null) continue;
                var idx = _orderedSelection.IndexOf(asset);
                cell.SetBadge(idx >= 0 ? idx + 1 : 0);
            }
        }
    }

    // -------------------------------------------------------
    // Collection view data source
    // -------------------------------------------------------

    private class VideoDataSource : UICollectionViewDataSource
    {
        private readonly OrderedVideoPickerController _controller;
        public VideoDataSource(OrderedVideoPickerController controller) => _controller = controller;

        public override nint GetItemsCount(UICollectionView collectionView, nint section)
            => _controller.FetchResult.Count;

        public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var cell = (VideoThumbnailCell)collectionView.DequeueReusableCell(VideoThumbnailCell.ReuseId, indexPath);
            var asset = _controller.FetchResult.ObjectAt(indexPath.Row) as PHAsset;
            if (asset == null) return cell;

            var selectionIndex = _controller.OrderedSelection.IndexOf(asset);
            cell.Configure(asset, selectionIndex >= 0 ? selectionIndex + 1 : 0);
            return cell;
        }
    }

    // -------------------------------------------------------
    // Collection view delegate
    // -------------------------------------------------------

    private class VideoCollectionDelegate : UICollectionViewDelegate
    {
        private readonly OrderedVideoPickerController _controller;
        public VideoCollectionDelegate(OrderedVideoPickerController controller) => _controller = controller;

        public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var asset = _controller.FetchResult.ObjectAt(indexPath.Row) as PHAsset;
            if (asset == null) return;
            var cell = collectionView.CellForItem(indexPath) as VideoThumbnailCell;
            if (cell == null) return;
            _controller.ToggleSelection(asset, cell);
        }
    }

    // -------------------------------------------------------
    // Thumbnail cell with number badge
    // -------------------------------------------------------

    private class VideoThumbnailCell : UICollectionViewCell
    {
        public static readonly NSString ReuseId = new("VideoThumbnailCell");

        private readonly UIImageView _imageView;
        private readonly UILabel _badge;
        private int _requestId;

        public VideoThumbnailCell(NativeHandle handle) : base(handle)
        {
            _imageView = new UIImageView(ContentView.Bounds)
            {
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
                ContentMode = UIViewContentMode.ScaleAspectFill,
                ClipsToBounds = true
            };

            _badge = new UILabel
            {
                BackgroundColor = UIColor.SystemBlue,
                TextColor = UIColor.White,
                Font = UIFont.BoldSystemFontOfSize(14),
                TextAlignment = UITextAlignment.Center,
                Hidden = true
            };
            _badge.Layer.CornerRadius = 12;
            _badge.Layer.MasksToBounds = true;

            ContentView.AddSubview(_imageView);
            ContentView.AddSubview(_badge);
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            _imageView.Frame = ContentView.Bounds;
            _badge.Frame = new CGRect(ContentView.Bounds.Width - 28, 4, 24, 24);
        }

        public void Configure(PHAsset asset, int badgeNumber)
        {
            // Cancel previous request
            if (_requestId != 0)
                PHImageManager.DefaultManager.CancelImageRequest(_requestId);

            var size = new CGSize(Bounds.Width * UIScreen.MainScreen.Scale, Bounds.Height * UIScreen.MainScreen.Scale);
            var options = new PHImageRequestOptions
            {
                DeliveryMode = PHImageRequestOptionsDeliveryMode.FastFormat,
                ResizeMode = PHImageRequestOptionsResizeMode.Fast
            };

            _requestId = PHImageManager.DefaultManager.RequestImageForAsset(
                asset, size, PHImageContentMode.AspectFill, options,
                (image, _) =>
                {
                    MainThread.BeginInvokeOnMainThread(() => _imageView.Image = image);
                });

            SetBadge(badgeNumber);
        }

        public void SetBadge(int number)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (number <= 0)
                {
                    _badge.Hidden = true;
                    ContentView.Alpha = 1f;
                }
                else
                {
                    _badge.Text = number.ToString();
                    _badge.Hidden = false;
                    ContentView.Alpha = 0.85f; // subtle selected dimming
                }
            });
        }
    }
}