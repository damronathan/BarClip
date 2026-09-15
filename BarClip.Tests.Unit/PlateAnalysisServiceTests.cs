using BarClip.Core.Services;
using BarClip.Models.Domain;
using BarClip.Models.Requests;
using static BarClip.Tests.Unit.OriginalVideoRequestTestExtensions;

namespace BarClip.Tests.Unit
{
    public class PlateAnalysisServiceTests
    {
        private readonly PlateAnalysisService _service;

        public PlateAnalysisServiceTests()
        {
            _service = new PlateAnalysisService();
        }

        [Fact]
        public void AnalyzeVideo_NoMovementDetected_ReturnsFullVideo()
        {
            var request = CreateRequest(TimeSpan.FromSeconds(20))
                .AddFrames(20, Detection(y: 200), Detection(x: 50, y: 200));

            var (start, finish) = _service.AnalyzeVideo(request);

            Assert.Equal(TimeSpan.Zero, start);
            Assert.Equal(request.Duration, finish);
        }

        [Fact]
        public void AnalyzeVideo_MovementAtFrame5And15_ReturnsTrimmedVideo()
        {
            var request = CreateRequest(TimeSpan.FromSeconds(20))
                .AddFrames(5, Detection(y: 200), Detection(x: 50, y: 200))
                .AddFrames(11, Detection(y: 250), Detection(x: 50, y: 250))
                .AddFrames(5, Detection(y: 200), Detection(x: 50, y: 200));

            var (start, finish) = _service.AnalyzeVideo(request);

            Assert.Equal(TimeSpan.FromSeconds(2.5), start);
            Assert.Equal(TimeSpan.FromSeconds(17), finish);
        }
        [Fact]
        public void AnalyzeVideo_MovementEveryFrame_TwelveFramesTwelveSecondDuration_ReturnsFullVideo()
        {
            var request = CreateRequest(TimeSpan.FromSeconds(12));

            for (int i = 0; i < 12; i++)
            {
                float y = i % 2 == 0 ? 200 : 250;
                request.AddFrames(1, Detection(y: y));
            }

            var (start, finish) = _service.AnalyzeVideo(request);

            Assert.Equal(TimeSpan.Zero, start);
            Assert.Equal(request.Duration, finish);
        }

        //Found that if start and end frame are the same, the start will be that frame - 2.5 and end will be the frame + 2 which will not break test.
        //the behavior will be to return a clip with the frame of movement with a buffer on each side. Rare edge case
        [Fact]
        public void AnalyzeVideo_SingleFrameOfMovmentDetected_FinishDoesNotExceedDuration()
        {
            var request = CreateRequest(TimeSpan.FromSeconds(10));

            request
                .AddFrames(5, Detection(y: 200))
                .AddFrames(5, Detection(y: 250));

            var (start, finish) = _service.AnalyzeVideo(request);

            Assert.True(finish <= request.Duration,
                $"Trim finish {finish} exceeded actual video duration {request.Duration}");
        }
        [Fact]
        public void AnalyzeVideo_ContinuousMovementThroughEndOfRecording_CapturesFullMovement()
        {
            var request = CreateRequest(TimeSpan.FromSeconds(10));

            request.AddFrames(5, Detection(y: 200));

            for (int i = 0; i < 5; i++)
            {
                float y = 216 + (i * 15);
                request.AddFrames(1, Detection(y: y));
            }

            var (start, finish) = _service.AnalyzeVideo(request);

            Assert.Equal(TimeSpan.FromSeconds(2.5), start);

            Assert.Equal(TimeSpan.FromSeconds(10), finish);
        }
        [Fact]
        public void AnalyzeVideo_VideoWithDurationLessThan1Second_SkipsTrim()
        {
            var request = CreateRequest(TimeSpan.FromSeconds(0.5));

            request.AddFrames(1);

            var (start, finish) = _service.AnalyzeVideo(request);

            Assert.Equal(TimeSpan.FromSeconds(0), start);

            Assert.Equal(TimeSpan.FromSeconds(0.5), finish);
        }
        

        
    }

    public static class OriginalVideoRequestTestExtensions
    {
        public static OriginalVideoRequest CreateRequest(TimeSpan duration)
        {
            return new OriginalVideoRequest
            {
                Id = Guid.NewGuid(),
                TrimStart = TimeSpan.Zero,
                TrimFinish = TimeSpan.Zero,
                CurrentTrimmedVideoId = Guid.NewGuid(),
                LifterFilter = LifterFilter.Whole,
                LiftNumber = 1,
                ThumbnailPath = "path/to/thumbnail.jpg",
                WeightKg = 0,
                Duration = duration,
                Frames = null,
                FilePath = "path/to/video.mp4",
                CompressedPath = "path/to/compressed_video.mp4"
            };
        }

        public static OriginalVideoRequest AddFrames(
            this OriginalVideoRequest request,
            int count,
            params PlateDetection[] detections)
        {
            request.Frames ??= new List<Frame>();

            int nextFrameNumber = request.Frames.Count == 0
                ? 0
                : request.Frames.Max(f => f.FrameNumber) + 1;

            for (int i = 0; i < count; i++)
            {
                request.Frames.Add(new Frame
                {
                    FrameNumber = nextFrameNumber,
                    FilePath = $"frame_{nextFrameNumber}.jpg",
                    PlateDetections = detections.ToList()
                });

                nextFrameNumber++;
            }

            return request;
        }


        public static PlateDetection Detection(
            float x = 100,
            float y = 200,
            float width = 50,
            float height = 20,
            float confidence = 0.95f)
        {
            return new PlateDetection
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Confidence = confidence
            };
        }
    }
}