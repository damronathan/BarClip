using Xabe.FFmpeg;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;


var frameData = await ExtractFrames(@"C:\BarClip.Main\repos\BarClip\BarClip.Console\Assets\206cj.mp4");

async Task<float[]> ExtractFrames(string path)
{
    IMediaInfo videoInfo = await FFmpeg.GetMediaInfo(path);
    IVideoStream videoStream = videoInfo.VideoStreams.First().SetCodec(VideoCodec.png);

    string outputFolder = @"C:\BarClip.Main\repos\BarClip\BarClip.Console\Assets";
    if (!Directory.Exists(outputFolder))
    {
        Directory.CreateDirectory(outputFolder);
    }
    foreach (var file in Directory.GetFiles(outputFolder, "frame_*.png"))
    {
        File.Delete(file);
    }
    Func<string, string> outputFileNameBuilder = number => Path.Combine(outputFolder, $"frame_{number}.png");
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
    List<float[]> filesAsFloats = new List<float[]>();
    foreach (var file in Directory.GetFiles(outputFolder, "frame_*.png")
                              .OrderBy(path =>
                              {
                                  string fileName = Path.GetFileNameWithoutExtension(path);
                                  var parts = fileName.Split('_');
                                  return int.TryParse(parts.Last(), out int n) ? n : int.MaxValue;
                              }))
    {
        try
        {
            float[] fileFloat = ConvertImage(file);
            await DetectPlates(fileFloat);
        }
        finally
        {
            File.Delete(file);
        }

    }
    float[] frameData = filesAsFloats.SelectMany(f => f).ToArray();
    int count = filesAsFloats.Count;
    Console.WriteLine($"{filesAsFloats.Count} floats created");
    Console.ReadLine();
    return frameData;
}

async Task DetectPlates(float[] frameData)
{
    using var session = new InferenceSession(@"C:\BarClip.Main\repos\BarClip\Models\PlateDetector.onnx");

    var dimensions = new[] { 1, 3, 640, 640 };

    var inputTensor = new DenseTensor<float>(frameData, dimensions);


    var input = NamedOnnxValue.CreateFromTensor("images", inputTensor);

    using var results = session.Run(new[] { input });
    foreach (var result in results)
    {
        Console.WriteLine($"Output name: {result.Name}");

        if (result.AsTensor<float>() is Tensor<float> outputTensor)
        {
            var outputArray = outputTensor.ToArray();
            int maxPrint = Math.Min(outputArray.Length, 20);

            Console.Write("Output values (first {0}): ", maxPrint);
            for (int i = 0; i < maxPrint; i++)
            {
                Console.Write(outputArray[i] + " ");
            }
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("Output is not a float tensor or cannot be cast to Tensor<float>.");
        }
    }
}

float[] ConvertImage(string path)
{
    using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(path);
    image.Mutate(x => x.Resize(640, 640));


    int width = image.Width;
    int height = image.Height;
    float[] data = new float[3 * width * height]; // CHW format

    int channelSize = width * height;

    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            var pixel = image[x, y];
            int idx = y * width + x;
            data[idx] = pixel.R / 255f;
            data[channelSize + idx] = pixel.G / 255f;
            data[2 * channelSize + idx] = pixel.B / 255f;
        }
    }


    return data;
};