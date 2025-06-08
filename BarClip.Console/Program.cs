using Xabe.FFmpeg;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using BarClip.Data.Schema;
using SixLabors.ImageSharp;
using Azure.Storage.Blobs;
using BarClip.Core.Services;

using var session = new InferenceSession(@"C:\Users\19139\runs\detect\train\weights\best.onnx");

string tempVideoPath = Path.GetTempPath();
string blobName;

string tempFramePath = Path.Combine(Path.GetTempPath(), "frames");

(tempVideoPath, blobName) = await DownloadOriginalVideo(tempVideoPath);
await VideoProcessorService.TrimVideo(tempVideoPath);
//await TrimVideo(tempVideoPath);

async Task TrimVideo(string path)
{
    Video video = new Video();
    video.FilePath = path;
    string outputPath = @"C:\ImageDetectorVideos\TrimmedVideos";
    Directory.CreateDirectory(outputPath);

    await ExtractFrames(video);
    var videoWithDetections = DetectPlates(video, tempFramePath);
    int startTime = GetTrimStart(video);
    int finishTime = GetTrimFinish(startTime, video);
    TimeSpan trimStart = TimeSpan.FromSeconds(startTime);
    TimeSpan trimFinish = TimeSpan.FromSeconds(finishTime);
    IMediaInfo videoInfo = await FFmpeg.GetMediaInfo(video.FilePath);
    IVideoStream videoStream = videoInfo.VideoStreams.First();
    IVideoStream trimmedVideoStream = videoStream.Split(trimStart, trimFinish - trimStart);
    var trimmedVideoPath = Path.Combine(outputPath, blobName);
    
    try 
    {
        var conversion = FFmpeg.Conversions.New()
            .AddStream(trimmedVideoStream)
            .SetOutput(trimmedVideoPath);
            
        var result = await conversion.Start();
        Console.WriteLine($"Video trimmed successfully. Output saved to: {trimmedVideoPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error trimming video: {ex.Message}");
        throw;
    }
}

async Task<(string, string)> DownloadOriginalVideo(string tempFilePath)
{
    string connectionString = "DefaultEndpointsProtocol=https;AccountName=barclipstorage;AccountKey=D4vHYa+EaLCffbGHQrH2OcmFoUOfCCu/XYBgQKs+D9ugjEHUmTL94I5CQGfAqCUwaYRZDptGhng/+AStd8QXQg==;EndpointSuffix=core.windows.net";
    var blobServiceClient = new BlobServiceClient(connectionString);

    string containerName = "originalvideos";
    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

    string blobName = "206cj.mp4";
    var blobClient = containerClient.GetBlobClient(blobName);

    string videoFilePath = Path.Combine(tempFilePath, blobName);

    await blobClient.DownloadToAsync(videoFilePath);

    return (videoFilePath, blobName);
}

async Task ExtractFrames(Video video)
{
    IMediaInfo videoInfo = await FFmpeg.GetMediaInfo(video.FilePath);
    IVideoStream videoStream = videoInfo.VideoStreams.First().SetCodec(VideoCodec.png);    

    Func<string, string> outputFileNameBuilder = number => Path.Combine(tempFramePath, $"frame_{number}.png");
    int fps = (int)Math.Round(videoStream.Framerate);

    try
    {
        await FFmpeg.Conversions.New()
            .AddStream(videoStream)
            .ExtractEveryNthFrame(fps, outputFileNameBuilder)
            .Start();

        Console.WriteLine("Frames extracted successfully.");

    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error extracting frames: {ex.Message}");
    }
}

NamedOnnxValue ConvertToOnnx(string framePath, string inputName = "images", int targetSize = 640)
{
    using var image = Image.Load<Rgb24>(framePath);

    float ratio = Math.Min((float)targetSize / image.Width, (float)targetSize / image.Height);
    int newWidth = (int)(image.Width * ratio);
    int newHeight = (int)(image.Height * ratio);

    image.Mutate(ctx => ctx.Resize(newWidth, newHeight));

    var paddedImage = new Image<Rgb24>(targetSize, targetSize);

    int padX = (targetSize - newWidth) / 2;
    int padY = (targetSize - newHeight) / 2;

    paddedImage.Mutate(ctx => ctx.DrawImage(image, new Point(padX, padY), 1f));

    int width = paddedImage.Width;
    int height = paddedImage.Height;
    float[] data = new float[3 * width * height];
    int channelSize = width * height;

    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            Rgb24 pixel = paddedImage[x, y];
            int idx = y * width + x;
            data[idx] = pixel.R / 255f;
            data[channelSize + idx] = pixel.G / 255f;
            data[2 * channelSize + idx] = pixel.B / 255f;
        }
    }

    var inputTensor = new DenseTensor<float>(data, new[] { 1, 3, height, width });
    return NamedOnnxValue.CreateFromTensor(inputName, inputTensor);
}

Video DetectPlates(Video video, string framePath)
{
    var detections = new List<PlateDetection>();
    int frameNumber = 0;
    foreach (var file in Directory.GetFiles(framePath, "frame_*.png").OrderBy(GetFrameNumber))
    {
        try
        {
            var frame = new Frame
            {
                FrameNumber = frameNumber,
                FilePath = file,
            };
            frameNumber++;

            NamedOnnxValue input = ConvertToOnnx(file);

            detections = RunInference(input);


            frame.PlateDetections = detections;

            video.Frames.Add(frame);

        }
        finally
        {
            File.Delete(file);
        }

    }
    return video;

}

List<PlateDetection> RunInference(NamedOnnxValue input)
{
    var plateDetections = new List<PlateDetection>();
    var detections = new List<(int index, float confidence, float xValue)>();

    using var outputs = session.Run([input]);
    var outputTensor = outputs[0].AsTensor<float>();

    for (int i = 0; i < 8400; i++)
    {
        float confidence = outputTensor[0, 4, i];
        float xValue = outputTensor[0, 0, i];

        if (confidence > 0.8f)
        {
            detections.Add((i, confidence, xValue));
        }
    }

    var groupedDetections = new List<List<(int index, float confidence, float xValue)>>();

    foreach (var detection in detections)
    {
        var existingGroup = groupedDetections.FirstOrDefault(
            group => group.Any(d => Math.Abs(d.xValue - detection.xValue) < 5f)
        );

        if (existingGroup != null)
        {
            existingGroup.Add(detection);
        }
        else
        {
            groupedDetections.Add(new List<(int, float, float)> { detection });
        }
    }

    foreach (var group in groupedDetections)
    {
        var bestDetection = group.OrderByDescending(d => d.confidence).First();

        var plateDetection = new PlateDetection
        {
            X = outputTensor[0, 0, bestDetection.index],
            Y = outputTensor[0, 1, bestDetection.index],
            Width = outputTensor[0, 2, bestDetection.index],
            Height = outputTensor[0, 3, bestDetection.index],
            Confidence = bestDetection.confidence,
            DetectionNumber = bestDetection.index

        };

        plateDetections.Add(plateDetection);
    }


    return plateDetections;

}

int GetTrimStart(Video video)
{
    float confidence = 0f;
    float yValue = new();
    int i = 0;
    int frameNumber = 0;
    PlateDetection plateDetection = new();
    List<PlateDetection> plateDetections = [];
    foreach (Frame frame in video.Frames)
    {
        foreach (PlateDetection detection in frame.PlateDetections)
        {
            if (i == 0 && detection.Confidence > confidence)
            {
                confidence = detection.Confidence;
                plateDetection = detection;
            }
            switch (frame.PlateDetections.Count)
            {
                case 0:
                    break;
                case 1:
                    plateDetection = detection;
                    break;
                default:
                    float targetX = plateDetection.X;
                    PlateDetection closestDetection = frame.PlateDetections
                        .OrderBy(pd => Math.Abs(pd.X - targetX))
                        .First();
                    plateDetection = closestDetection;
                    break;
            }
        }
        if (i == 0)
        {
            yValue = plateDetection.Y;
            i = 1;
        }
        if (plateDetection.Y > yValue + 50f
            || plateDetection.Y < yValue - 50f
            && i == 1)
        {
            frameNumber = frame.FrameNumber;
            i = 2;
        }
    }
    return frameNumber - 2;
}

int GetTrimFinish(int trim, Video video)
{
    float confidence = 0f;
    float yValue = new();
    int consecutiveStableFrames = 0;
    int frameNumber = trim;
    PlateDetection plateDetection = new();
    List<PlateDetection> plateDetections = [];

    for (int i = trim; i < video.Frames.Count; i++)
    {
        Frame frame = video.Frames[i];
        foreach (PlateDetection detection in frame.PlateDetections)
        {
            if (i == trim && detection.Confidence > confidence)
            {
                confidence = detection.Confidence;
                plateDetection = detection;
            }
            switch (frame.PlateDetections.Count)
            {
                case 0:
                    break;
                case 1:
                    plateDetection = detection;
                    break;
                default:
                    float targetX = plateDetection.X;
                    PlateDetection closestDetection = frame.PlateDetections
                        .OrderBy(pd => Math.Abs(pd.X - targetX))
                        .First();
                    plateDetection = closestDetection;
                    break;
            }
        }

        if (i == trim)
        {
            yValue = plateDetection.Y;
        }
        else
        {
            if (Math.Abs(plateDetection.Y - yValue) < 10f)
            {
                consecutiveStableFrames++;
                if (consecutiveStableFrames >= 3)
                {
                    return frame.FrameNumber - 2;
                }
            }
            else
            {
                consecutiveStableFrames = 0;
            }
            yValue = plateDetection.Y;
        }
    }
    
    return video.Frames.Count - 1;
}

static int GetFrameNumber(string path)
{
    string fileName = Path.GetFileNameWithoutExtension(path);
    var parts = fileName.Split('_');
    return int.TryParse(parts.Last(), out int n) ? n : int.MaxValue;
}