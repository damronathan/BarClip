using BarClip.Data.Schema;
using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace BarClip.Core.Services;

public class DetectionService
{
    public static List<Frame> DetectPlates(string framePath)
    {
        var session = new InferenceSession(@"C:\Users\19139\runs\detect\train\weights\best.onnx");
        var frames = new List<Frame>();
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

                NamedOnnxValue input = ConvertFrameToOnnxValue(file);

                detections = RunInference(input, session);

                frame.PlateDetections = detections;

                frames.Add(frame);

            }
            finally
            {
                File.Delete(file);
            }

        }
        return frames;

    }

    private static NamedOnnxValue ConvertFrameToOnnxValue(string framePath)
    {
        using var image = Image.Load<Rgb24>(framePath);

        int targetSize = 640;

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

        var inputTensor = new DenseTensor<float>(data, [1, 3, height, width]);
        return NamedOnnxValue.CreateFromTensor("images", inputTensor);
    }

    private static List<PlateDetection> RunInference(NamedOnnxValue input, InferenceSession session)
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

    private static int GetFrameNumber(string path)
    {
        string fileName = Path.GetFileNameWithoutExtension(path);
        var parts = fileName.Split('_');
        return int.TryParse(parts.Last(), out int n) ? n : int.MaxValue;
    }
}
