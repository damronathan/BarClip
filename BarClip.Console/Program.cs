using Xabe.FFmpeg;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Numerics;

// Initialize paths
Video video = new Video
{
    FilePath = @"C:\BarClip.Main\repos\BarClip\BarClip.Console\Assets\206cj.mp4",
    OutputPath = @"C:\BarClip.Main\repos\BarClip\BarClip.Console\Assets\Frames"
};
var session = new InferenceSession(@"C:\Users\19139\runs\detect\train\weights\best.onnx");

Console.ReadLine();
await ExtractFrames(video);
await DetectAndVisualize(video);

async Task ExtractFrames(Video video)
{
    var videoInfo = await FFmpeg.GetMediaInfo(video.FilePath);
    var videoStream = videoInfo.VideoStreams.First().SetCodec(VideoCodec.png);

    if (!Directory.Exists(video.OutputPath))
        Directory.CreateDirectory(video.OutputPath);

    foreach (var file in Directory.GetFiles(video.OutputPath, "frame_*.png"))
        File.Delete(file);

    Func<string, string> outputFileNameBuilder = number => Path.Combine(video.OutputPath, $"frame_{number}.png");
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

async Task DetectAndVisualize(Video video)
{
    int frameNumber = 1;
    foreach (var file in Directory.GetFiles(video.OutputPath, "frame_*.png")
                              .OrderBy(path =>
                              {
                                  string fileName = Path.GetFileNameWithoutExtension(path);
                                  var parts = fileName.Split('_');
                                  return int.TryParse(parts.Last(), out int n) ? n : int.MaxValue;
                              }))
    {
        try
        {
            using (var image = Image.Load<Rgba32>(file))
            {
                // Store original dimensions
                int originalWidth = image.Width;
                int originalHeight = image.Height;

                NamedOnnxValue input = ConvertImageWithLetterbox(file);
                var detections = GetDetections(input);

                // Draw only bounding boxes
                foreach (var det in detections)
                {
                    // Convert coordinates back to original image space
                    float ratio = Math.Min(640f / originalWidth, 640f / originalHeight);
                    int newWidth = (int)(originalWidth * ratio);
                    int newHeight = (int)(originalHeight * ratio);
                    int padX = (640 - newWidth) / 2;
                    int padY = (640 - newHeight) / 2;

                    // Remove padding and scale back to original image size
                    float x = (det.X - padX) / ratio;
                    float y = (det.Y - padY) / ratio;
                    float width = det.Width / ratio;
                    float height = det.Height / ratio;

                    var rect = new RectangleF(x, y, width, height);
                    image.Mutate(ctx =>
                    {
                        ctx.Draw(Pens.Solid(Color.Red, 3), rect);
                    });
                }

                image.Save(file); // Overwrite the frame with boxes
            }

            Console.WriteLine($"Frame {frameNumber} processed: detections.");
        }
        finally
        {
            frameNumber++;
        }
    }
}

NamedOnnxValue ConvertImageWithLetterbox(string imagePath, string inputName = "images", int targetSize = 640)
{
    using var image = Image.Load<Rgb24>(imagePath);

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

List<Detection> GetDetections(NamedOnnxValue input, float confThreshold = 0.25f, float iouThreshold = 0.45f)
{
    using var outputs = session.Run([input]);
    var outputTensor = outputs.First().AsTensor<float>();
    var dims = outputTensor.Dimensions; // [1, num_boxes, 85]
    int numDetections = dims[1];
    int numAttributes = dims[2];
    float[] data = outputTensor.ToArray();

    var detections = new List<Detection>();

    for (int i = 0; i < numDetections; i++)
    {
        int offset = i * numAttributes;
        float objConf = 1f / (1f + (float)Math.Exp(-data[offset + 4]));
        if (objConf < confThreshold) continue;

        float maxClassConf = 0f;
        int maxClass = -1;
        for (int j = 5; j < numAttributes; j++)
        {
            float classConf = 1f / (1f + (float)Math.Exp(-data[offset + j]));
            if (classConf > maxClassConf)
            {
                maxClassConf = classConf;
                maxClass = j - 5;
            }
        }

        float finalConf = objConf * maxClassConf;
        if (finalConf < confThreshold) continue;

        // The model outputs coordinates in the range [0, 1]
        float cx = data[offset];
        float cy = data[offset + 1];
        float w = data[offset + 2];
        float h = data[offset + 3];

        // Convert to pixel coordinates
        float x = (cx - w/2) * 640;  // Convert to pixel coordinates
        float y = (cy - h/2) * 640;
        float width = w * 640;
        float height = h * 640;

        detections.Add(new Detection
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Confidence = finalConf,
            ClassId = maxClass
        });
    }

    return NMS(detections, iouThreshold);
}

List<Detection> NMS(List<Detection> detections, float iouThreshold)
{
    var finalDetections = new List<Detection>();
    var sorted = detections.OrderByDescending(d => d.Confidence).ToList();

    Console.WriteLine($"Before NMS: {sorted.Count} detections");
    foreach (var det in sorted)
    {
        Console.WriteLine($"Detection - Conf: {det.Confidence:F4}, Box: ({det.X:F1}, {det.Y:F1}, {det.Width:F1}, {det.Height:F1})");
    }

    while (sorted.Any())
    {
        var current = sorted.First();
        finalDetections.Add(current);
        sorted.RemoveAt(0);

        // Keep only detections with IoU less than threshold
        sorted = sorted.Where(d => 
        {
            float iou = IoU(current, d);
            Console.WriteLine($"IoU between detections: {iou:F4}");
            return iou < iouThreshold;
        }).ToList();
    }

    Console.WriteLine($"After NMS: {finalDetections.Count} detections");
    return finalDetections;
}

float IoU(Detection a, Detection b)
{
    // Calculate intersection rectangle
    float x1 = Math.Max(a.X, b.X);
    float y1 = Math.Max(a.Y, b.Y);
    float x2 = Math.Min(a.X + a.Width, b.X + b.Width);
    float y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

    // Calculate intersection area
    float intersection = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
    
    // Calculate union area
    float areaA = a.Width * a.Height;
    float areaB = b.Width * b.Height;
    float union = areaA + areaB - intersection;

    // Avoid division by zero
    if (union <= 0) return 0;

    return intersection / union;
}

class Detection
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public float Confidence { get; set; }
    public int ClassId { get; set; }
}

class Video
{
    public string FilePath { get; set; }
    public string OutputPath { get; set; } = @"C:\Temp\ExtractedFrames";
}
