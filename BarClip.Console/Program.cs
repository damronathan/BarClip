using Xabe.FFmpeg;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Runtime.Versioning;
using BarClip.Data.Schema;
using SixLabors.ImageSharp;


Video video = new Video();
video.FilePath = @"C:\BarClip.Main\repos\BarClip\BarClip.Console\Assets\206cj.mp4";
using var session = new InferenceSession(@"C:\Users\19139\runs\detect\train\weights\best.onnx");


Console.ReadLine();
await ExtractFrames(video);
await DetectPlates(video);

async Task ExtractFrames(Video video)
{
    IMediaInfo videoInfo = await FFmpeg.GetMediaInfo(video.FilePath);
    IVideoStream videoStream = videoInfo.VideoStreams.First().SetCodec(VideoCodec.png);

    if (!Directory.Exists(video.OutputPath))
    {
        Directory.CreateDirectory(video.OutputPath);
    }
    foreach (var file in Directory.GetFiles(video.OutputPath, "frame_*.png"))
    {
        File.Delete(file);
    }
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

async Task DetectPlates(Video video)
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
            var frame = new Frame
            {
                FrameNumber = frameNumber,
                FilePath = file
            };
            video.Frames.Add(frame);

            frameNumber++;
            NamedOnnxValue input = ConvertImageWithLetterbox(file);
            using var outputs = session.Run([input]);

            foreach (var output in outputs)
            {
                using (output)
                {
                    var outputTensor = output.AsTensor<float>();
                    float[] rawOutput = outputTensor.ToArray();

                    int detectionSize = 5;
                    int numDetections = detectionSize;
                        var plateDetection = new PlateDetection
                        {
                            CenterX = rawOutput[0],
                            CenterY = rawOutput[1],
                            Width = rawOutput[2],
                            Height = rawOutput[3],
                            Confidence = rawOutput[4],
                            Frame = frame,
                            FrameId = frame.Id
                        };
                        
                    
                }
            }

        }
        finally
        {
            File.Delete(file);
        }
    }
}

NamedOnnxValue ConvertImageWithLetterbox(string imagePath, string inputName = "images", int targetSize = 640)
{
    using var image = Image.Load<Rgb24>(imagePath);

    // Calculate resize ratio (preserve aspect ratio)
    float ratio = Math.Min((float)targetSize / image.Width, (float)targetSize / image.Height);
    int newWidth = (int)(image.Width * ratio);
    int newHeight = (int)(image.Height * ratio);

    // Resize the image with aspect ratio preserved
    image.Mutate(ctx => ctx.Resize(newWidth, newHeight));

    // Create a black image with target size (640x640)
    var paddedImage = new Image<Rgb24>(targetSize, targetSize);

    // Calculate padding to center the image
    int padX = (targetSize - newWidth) / 2;
    int padY = (targetSize - newHeight) / 2;

    // Paste resized image onto the black canvas at centered position
    paddedImage.Mutate(ctx => ctx.DrawImage(image, new Point(padX, padY), 1f));

    // Prepare float array for tensor (channels first: 3 x 640 x 640)
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