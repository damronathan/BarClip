using Microsoft.Extensions.Logging;
using BarClip.Core;
using Microsoft.Extensions.Configuration;
using BarClip.Data;
using Microsoft.EntityFrameworkCore;
using SkiaSharp.Views.Maui.Controls.Hosting;
using BarClip.Core.Interfaces;
#if IOS
using BarClip.Maui.Platforms.iOS.Services;
#elif WINDOWS
using BarClip.Maui.Platforms.Windows.Services;
#endif

namespace BarClip.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder

            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseSentry(options => {
                // The DSN is the only required setting.
                options.Dsn = "https://0c5952290b452cf311494a6a5a455c1d@o4511021185630208.ingest.us.sentry.io/4511021190283264";

                // Use debug mode if you want to see what the SDK is doing.
                // Debug messages are written to stdout with Console.Writeline,
                // and are viewable in your IDE's debug console or with 'adb logcat', etc.
                // This option is not recommended when deploying your application.
                options.Debug = true;

                // Other Sentry options can be set here.
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Setup SQLite connection string and model path
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "barclip.db");
        var modelPath = Path.Combine(FileSystem.AppDataDirectory, "PlateDetector.onnx");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={dbPath}",
                ["OnnxModelOptions:Path"] = modelPath
            })
            .Build();

        builder.Configuration.AddConfiguration(configuration);
        builder.Services.RegisterMauiServices(builder.Configuration);
#if WINDOWS
        builder.Services.AddScoped<IVideoEditor, WindowsVideoEditor>();
#elif IOS
        builder.Services.AddScoped<IVideoEditor, IOSVideoEditor>();
#endif

        // Register pages
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<SessionLibrary>();
        builder.Services.AddTransient<SessionPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Initialize database and extract model
        try
        {
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                System.Diagnostics.Debug.WriteLine($"Database path: {dbPath}");


                dbContext.Database.Migrate();

                // Check if tables exist
                var tables = dbContext.Model.GetEntityTypes().Select(t => t.GetTableName()).ToList();
                System.Diagnostics.Debug.WriteLine($"Entity types found: {string.Join(", ", tables)}");
            }

            // Extract model if it doesn't exist
            if (!File.Exists(modelPath))
            {
                ExtractOnnxModel().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Initialization error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        return app;
    }

    private static async Task ExtractOnnxModel()
    {
        var targetPath = Path.Combine(FileSystem.AppDataDirectory, "PlateDetector.onnx");

        using var stream = await FileSystem.OpenAppPackageFileAsync("PlateDetector.onnx");
        using var fileStream = File.Create(targetPath);
        await stream.CopyToAsync(fileStream);
    }
}