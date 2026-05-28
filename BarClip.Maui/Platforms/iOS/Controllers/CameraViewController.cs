using AVFoundation;
using CoreMedia;
using Foundation;
using UIKit;

public class CameraViewController : UIViewController
{
    private readonly TaskCompletionSource<FileResult?> _tcs;
    private AVCaptureSession _session;
    private AVCaptureMovieFileOutput _movieOutput;
    private AVCaptureVideoPreviewLayer _previewLayer;
    private UIButton _recordButton;
    private bool _isRecording = false;
    private string _filePath;

    public CameraViewController(TaskCompletionSource<FileResult?> tcs)
    {
        _tcs = tcs;
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        View.BackgroundColor = UIColor.Black;
        SetupCamera();
        SetupUI();
    }

    public override void ViewDidLayoutSubviews()
    {
        base.ViewDidLayoutSubviews();
        _previewLayer.Frame = View.Bounds;
    }

    public override void ViewWillDisappear(bool animated)
    {
        base.ViewWillDisappear(animated);
        if (_isRecording)
            _movieOutput.StopRecording();
        _session.StopRunning();
    }

    private void SetupCamera()
    {
        _session = new AVCaptureSession();

        var camera = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video);
        if (camera == null) { _tcs.TrySetResult(null); return; }

        NSError error;
        camera.LockForConfiguration(out error);
        if (error == null)
        {
            var formats60 = new List<AVCaptureDeviceFormat>();

            foreach (var format in camera.Formats)
            {
                foreach (var range in format.VideoSupportedFrameRateRanges)
                {
                    if (range.MaxFrameRate >= 60)
                    {
                        formats60.Add(format);
                        break;
                    }
                }
            }

            var selectedFormat = formats60.LastOrDefault();
            if (selectedFormat != null)
            {
                camera.ActiveFormat = selectedFormat;
                camera.ActiveVideoMinFrameDuration = new CMTime(1, 60);
                camera.ActiveVideoMaxFrameDuration = new CMTime(1, 60);
            }

            camera.UnlockForConfiguration();
        }

        var cameraInput = AVCaptureDeviceInput.FromDevice(camera, out error);
        if (cameraInput != null && _session.CanAddInput(cameraInput))
            _session.AddInput(cameraInput);

        var mic = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Audio);
        if (mic != null)
        {
            var micInput = AVCaptureDeviceInput.FromDevice(mic, out error);
            if (micInput != null && _session.CanAddInput(micInput))
                _session.AddInput(micInput);
        }

        _movieOutput = new AVCaptureMovieFileOutput();
        if (_session.CanAddOutput(_movieOutput))
            _session.AddOutput(_movieOutput);

        _previewLayer = new AVCaptureVideoPreviewLayer(_session);
        _previewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
        View.Layer.AddSublayer(_previewLayer);

        _session.StartRunning();
    }
    private void SetupUI()
    {
        // Cancel button
        var cancelButton = new UIButton(UIButtonType.System);
        cancelButton.SetTitle("Cancel", UIControlState.Normal);
        cancelButton.SetTitleColor(UIColor.White, UIControlState.Normal);
        cancelButton.TitleLabel.Font = UIFont.SystemFontOfSize(18);
        cancelButton.TranslatesAutoresizingMaskIntoConstraints = false;
        cancelButton.TouchUpInside += (s, e) =>
        {
            DismissViewController(true, null);
            _tcs.TrySetResult(null);
        };
        View.AddSubview(cancelButton);

        // Record button
        _recordButton = new UIButton(UIButtonType.Custom);
        _recordButton.BackgroundColor = UIColor.Red;
        _recordButton.Layer.CornerRadius = 35;
        _recordButton.Layer.BorderWidth = 4;
        _recordButton.Layer.BorderColor = UIColor.White.CGColor;
        _recordButton.TranslatesAutoresizingMaskIntoConstraints = false;
        _recordButton.TouchUpInside += OnRecordButtonTapped;
        View.AddSubview(_recordButton);

        NSLayoutConstraint.ActivateConstraints(new[]
        {
            cancelButton.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor, 16),
            cancelButton.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 16),

            _recordButton.BottomAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.BottomAnchor, -40),
            _recordButton.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
            _recordButton.WidthAnchor.ConstraintEqualTo(70),
            _recordButton.HeightAnchor.ConstraintEqualTo(70),
        });
    }

    private void OnRecordButtonTapped(object sender, EventArgs e)
    {
        if (_isRecording)
        {
            _movieOutput.StopRecording();
            _recordButton.BackgroundColor = UIColor.Red;
            _isRecording = false;
        }
        else
        {
            _filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".MOV");
            var fileUrl = NSUrl.FromFilename(_filePath);
            var delegate_ = new RecordingDelegate(_tcs, this);
            _movieOutput.StartRecordingToOutputFile(fileUrl, delegate_);
            _recordButton.BackgroundColor = UIColor.FromRGB(180, 0, 0);
            _isRecording = true;
        }
    }

    private class RecordingDelegate : AVCaptureFileOutputRecordingDelegate
    {
        private readonly TaskCompletionSource<FileResult?> _tcs;
        private readonly CameraViewController _vc;

        public RecordingDelegate(TaskCompletionSource<FileResult?> tcs, CameraViewController vc)
        {
            _tcs = tcs;
            _vc = vc;
        }

        public override void FinishedRecording(AVCaptureFileOutput captureOutput, NSUrl outputFileUrl, NSObject[] connections, NSError error)
        {
            if (error != null)
            {
                SentrySdk.CaptureException(new Exception(error.LocalizedDescription));
                _tcs.TrySetResult(null);
            }
            else
            {
                _tcs.TrySetResult(new FileResult(outputFileUrl.Path!));
            }

            _vc.DismissViewController(true, null);
        }
    }
}