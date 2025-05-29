using BarClip.Data.Schema;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace BarClip.Core.Services
{
    public class MediaService
    {
        public async Task ExtractFrames(Video video)
        {          
            List<Frame> frames = new List<Frame>();

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
            int fps = (int)Math.Round(video.FrameRate);

            try
            {
                await FFmpeg.Conversions.New()
                    .AddStream(video.VideoStream)
                    .ExtractEveryNthFrame(fps, outputFileNameBuilder)
                    .Start();

                Console.WriteLine("Frames extracted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting frames: {ex.Message}");
            }
        }

        public float[] ConvertImage(string path)
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
        }
        public static async Task<Video> CreateVideo(string path)
        {
            IMediaInfo videoInfo = await FFmpeg.GetMediaInfo(path);
            IVideoStream videoStream = videoInfo.VideoStreams.First().SetCodec(VideoCodec.png);
            Video video = new()
            {
                Id = Guid.NewGuid(),
                FilePath = path,
                Duration = videoInfo.Duration,
                FrameRate = videoStream.Framerate,
                Status = VideoStatus.Uploaded,
                VideoStream = videoStream
            };
            return video;
        }
        
        public async Task<float[]> CreateFrameTensorData()
        {

        }

    }
}
