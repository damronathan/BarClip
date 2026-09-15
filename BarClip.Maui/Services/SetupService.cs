using BarClip.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BarClip.Maui.Services;

public class SetupService
{
    private readonly IServiceScopeFactory _scopeFactory;

    private bool _isInitialized;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SetupService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _lock.WaitAsync();
        try
        {
            if (_isInitialized) return; // re-check after acquiring lock

            await MigrateDatabaseAsync();
            await EnsureOnnxModelExtractedAsync();

            _isInitialized = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task MigrateDatabaseAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Database.MigrateAsync();

            var tables = dbContext.Model.GetEntityTypes().Select(t => t.GetTableName()).ToList();
            System.Diagnostics.Debug.WriteLine($"Entity types found: {string.Join(", ", tables)}");
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            SentrySdk.Flush(TimeSpan.FromSeconds(3));
            System.Diagnostics.Debug.WriteLine($"DB migration error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw; // let the caller decide how to surface this to the UI
        }
    }

    private async Task EnsureOnnxModelExtractedAsync()
    {
        var modelPath = Path.Combine(FileSystem.AppDataDirectory, "PlateDetector.onnx");

        if (File.Exists(modelPath))
            return;

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("PlateDetector.onnx");
            using var fileStream = File.Create(modelPath);
            await stream.CopyToAsync(fileStream);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            SentrySdk.Flush(TimeSpan.FromSeconds(3));
            System.Diagnostics.Debug.WriteLine($"ONNX extraction error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}