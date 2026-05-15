namespace BarClip.Maui.Services
{
    public static class CacheService
    {
        public static void MoveToCache(string path, Guid id)
        {
            var fileName = $"{id}.MOV";
            File.Move(path, Path.Combine(FileSystem.CacheDirectory, fileName));
        }
        public static async Task<string> DownloadToCacheAsync(string url, Guid id)
        {
            var cachePath = Path.Combine(FileSystem.CacheDirectory, $"{id}.MOV");

            if (File.Exists(cachePath))
                return cachePath;

            using var client = new HttpClient();
            var bytes = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(cachePath, bytes);

            return cachePath;
        }
        public static async Task<string> DownloadThumbnailToCacheAsync(string url, Guid id)
        {
            var cachePath = Path.Combine(FileSystem.CacheDirectory, $"{id}.jpg");

            if (File.Exists(cachePath))
                return cachePath;

            using var client = new HttpClient();
            var bytes = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(cachePath, bytes);

            return cachePath;
        }
    }
}
