using BarClip.Models.Requests;

namespace BarClip.Core.Helpers;
public class FileHelper
{
    public static SessionFolderPaths CreateSessionFolders(string basePath, Guid sessionId)
    {
        var sessionPath = Path.Combine(basePath, sessionId.ToString());

        return new SessionFolderPaths
        {
            Session = sessionPath,
            Thumbnails = Path.Combine(sessionPath, "Thumbnails"),
            Original = Path.Combine(sessionPath, "Original"),
            Processed = Path.Combine(sessionPath, "Processed"),
        };
    }
    public static void SortVideoAndThumbnail(
        string videoPath,
        string thumbnailPath,
        SessionFolderPaths sessionFolderPaths,
        Guid originalVideoId)
    {

        File.Move(videoPath, Path.Combine(sessionFolderPaths.Original, $"{originalVideoId}.MOV"));
        File.Move(thumbnailPath, Path.Combine(sessionFolderPaths.Thumbnails, $"{originalVideoId}.png"));
    }

    public static async Task<string> PrepareFilesForMerge(SessionFolderPaths sessionFolderPaths)
    {
        var processedFiles = Directory.GetFiles(sessionFolderPaths.Processed)
                            .Where(f => int.TryParse(Path.GetFileNameWithoutExtension(f).Split('_')[0], out _))
                            .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f).Split('_')[0]))
                            .ToList();
        if (!processedFiles.Any())
        {
            throw new ArgumentException("No processed videos found to merge");
        }

        var concatListPath = Path.Combine(sessionFolderPaths.Processed, "file_list.txt");
        await using (var writer = new StreamWriter(concatListPath))
        {
            foreach (var file in processedFiles)
            {
                writer.WriteLine($"file '{file.Replace("'", "'\\''")}'");
            }
        }


        return concatListPath;
    }
    public class SessionFolderPaths
    {
        public string Session { get; set; }
        public string Thumbnails { get; set; }
        public string Processed { get; set; }
        public string Original { get; set; }
    }
}
