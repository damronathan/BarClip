namespace BarClip.Maui.Services
{
    public static class CacheService
    {
        public static void MoveToCache(string path, Guid id)
        {
            var fileName = $"{id}.MOV";
            File.Move(path, Path.Combine(FileSystem.CacheDirectory, fileName));
        }
    }
}
