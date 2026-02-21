using BarClip.Data.Schema;
using BarClip.Models.Requests;
using FFMpegCore;
using FFMpegCore.Enums;
using System.Diagnostics;
using static BarClip.Core.Helpers.FileHelper;

namespace BarClip.Maui.Platforms.Windows.Helpers;
public class FFMpegHelper
{
    public static async Task ExtractAllFramesAsync(OriginalVideoRequest originalVideo, string tempFramePath)
    {
        await FFMpegArguments
                .FromFileInput(originalVideo.FilePath)
                .OutputToFile(Path.Combine(tempFramePath, "frame_%d.png"), overwrite: true, options => options
                    .WithVideoFilters(filterOptions => filterOptions
                        .Scale(VideoSize.Original))
                    .WithCustomArgument("-vf fps=1 -q:v 5")
                    .WithCustomArgument("-threads 0"))
                .ProcessAsynchronously();
    }

    public static async Task TrimAsync(OriginalVideoRequest originalVideo, ProcessedVideoRequest trimmedVideo)
    {
        await FFMpegArguments
                .FromFileInput(originalVideo.FilePath, true, options => options
                .Seek(originalVideo.TrimStart))
                .OutputToFile(trimmedVideo.FilePath, overwrite: true, options => options
                .WithDuration(trimmedVideo.Duration)
                .WithCustomArgument("-c:v copy -c:a aac"))
                .ProcessAsynchronously();
    }

    public static async Task TrimAndLabelAsync(OriginalVideoRequest originalVideo, ProcessedVideoRequest processedVideo, string? weightText)
    {
        if (weightText is not null)
        {
            await FFMpegArguments
        .FromFileInput(originalVideo.FilePath, true, options => options.Seek(originalVideo.TrimStart))

        .OutputToFile(processedVideo.FilePath, overwrite: true, options => options
            .WithDuration(processedVideo.Duration)
            .WithCustomArgument($"-vf drawtext=text='{weightText}':fontfile='C\\:/Windows/Fonts/arial.ttf':fontcolor=white:bordercolor=black:borderw=16:fontsize=85:x=w-text_w-10:y=h-text_h-20")
            .WithCustomArgument("-c:v hevc_nvenc -color_primaries bt2020 -color_trc arib-std-b67 -colorspace bt2020nc")
            .WithCustomArgument("-preset p4")
            .WithCustomArgument("-cq 20")
            .WithCustomArgument("-pix_fmt yuv420p")
            .WithCustomArgument("-c:a aac")
            .WithCustomArgument("-r 60")
            .WithCustomArgument("-movflags +faststart"))
    .ProcessAsynchronously();
        }
        else
        {
            await FFMpegArguments
        .FromFileInput(originalVideo.FilePath, true, options => options.Seek(originalVideo.TrimStart))

        .OutputToFile(processedVideo.FilePath, overwrite: true, options => options
            .WithDuration(processedVideo.Duration)
            .WithCustomArgument("-c:v hevc_nvenc -color_primaries bt2020 -color_trc arib-std-b67 -colorspace bt2020nc")
            .WithCustomArgument("-preset p4")
            .WithCustomArgument("-cq 20")
            .WithCustomArgument("-pix_fmt yuv420p")
            .WithCustomArgument("-c:a aac")
            .WithCustomArgument("-r 60")
            .WithCustomArgument("-movflags +faststart"))
    .ProcessAsynchronously();
        }
        
    }

    public static async Task ExtractSingleFrameAsync(string originalFilePath, double thumbnailTime, string thumbnailPath)
    {
        await FFMpegArguments
        .FromFileInput(originalFilePath, true, options => options.Seek(TimeSpan.FromSeconds(thumbnailTime)))
        .OutputToFile(thumbnailPath, overwrite: true, options => options
            .WithCustomArgument("-frames:v 1 -q:v 5")
            .WithCustomArgument("-threads 0"))
        .ProcessAsynchronously();
    }

    public static async Task<(string stdout, string stderr)> RunFFmpegAsync(string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        var tcs = new TaskCompletionSource<bool>();
        process.Exited += (s, e) => tcs.TrySetResult(true);

        process.Start();

        var stderrTask = process.StandardError.ReadToEndAsync();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();

        await tcs.Task;

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return (stdout, stderr);
    }

    public async static Task<string> MergeVideos(SessionFolderPaths sessionFolderPaths, Guid sessionId)
    {
        var concatListPath = await PrepareFilesForMerge(sessionFolderPaths);

        var finalOutputPath = Path.Combine(sessionFolderPaths.Session, $"FullSession{sessionId}.MOV");

        var arguments = $"-y -f concat -safe 0 -i \"{concatListPath}\" -c copy \"{finalOutputPath}\"";

        var (stdout, stderr) = await FFMpegHelper.RunFFmpegAsync(arguments);

        return finalOutputPath;
    }

    public async static Task<string[]> ExtractThumbnails(string originalFolderPath, string thumbnailFolderPath)
    {
        var originalFilePaths = Directory.GetFiles(originalFolderPath, "*.MOV");

        foreach (string originalFilePath in originalFilePaths)
        {
            string fileName = Path.GetFileNameWithoutExtension(originalFilePath);
            string thumbnailPath = Path.Combine(thumbnailFolderPath, fileName + ".png");
            var videoAnalysis = await FFProbe.AnalyseAsync(originalFilePath);
            var thumbnailTime = videoAnalysis.Duration.TotalSeconds / 2;

            try
            {
                await FFMpegHelper.ExtractSingleFrameAsync(originalFilePath, thumbnailTime, thumbnailPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error extracting thumbnail from {originalFilePath}: {ex.Message}");
            }
        }
        return originalFilePaths;
    }

    // Not in use anymore. Keeping for reference.
    //public double TestDuration(string originalFolderPath)
    //{
    //    double totalDuration = new();
    //    foreach (var file in Directory.GetFiles(originalFolderPath))
    //    {
    //        var analysis = FFProbe.Analyse(file);
    //        totalDuration += analysis.Duration.TotalSeconds;
    //        Console.WriteLine($"file {file} duration = {analysis.Duration}. Total Duration = {totalDuration}");
    //    }
    //    return totalDuration;
    //}

}