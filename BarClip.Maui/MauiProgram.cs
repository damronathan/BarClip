using Microsoft.Extensions.Logging;
using BarClip.Core;
using Microsoft.Extensions.Configuration;
using BarClip.Data;
using Microsoft.EntityFrameworkCore;
using SkiaSharp.Views.Maui.Controls.Hosting;
using BarClip.Core.Interfaces;
using Microsoft.Identity.Client;
using CommunityToolkit.Maui;

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
        SentrySdk.Init(options =>
        {
            options.Dsn = "https://0c5952290b452cf311494a6a5a455c1d@o4511021185630208.ingest.us.sentry.io/4511021190283264";
            options.Debug = true;
        });
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            SentrySdk.CaptureException(args.ExceptionObject as Exception);
            SentrySdk.Flush(TimeSpan.FromSeconds(3));
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            SentrySdk.CaptureException(args.Exception);
            SentrySdk.Flush(TimeSpan.FromSeconds(3));
            args.SetObserved();
        };
        var builder = MauiApp.CreateBuilder();
        var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult();
        builder.Configuration.AddJsonStream(stream);
        //    var config = new ConfigurationBuilder()
        //.AddJsonFile("appsettings.json", optional: true)
        //.Build();

        builder

            .UseMauiApp<App>()
            .UseMauiCommunityToolkitMediaElement()
            .UseSkiaSharp()
            .UseSentry(options =>
            {
                options.Dsn = builder.Configuration["Sentry:Dsn"];
                options.Debug = true;
                options.SendDefaultPii = true;
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
        builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
        try
        {
            var authConfig = builder.Configuration.GetSection("AzureAd");

            var pca = PublicClientApplicationBuilder
                .Create(authConfig["ClientId"])
                .WithAuthority($"https://barclip.ciamlogin.com/barclip.onmicrosoft.com/SignUpSignIn")
                .WithRedirectUri($"msal{authConfig["ClientId"]}://auth")
                .WithIosKeychainSecurityGroup("com.nathandamron.barclip")
                .Build();

            builder.Services.AddSingleton<IPublicClientApplication>(pca);
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddHttpClient<ApiClientService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(120);
            }); 
            builder.Services.AddSingleton<ApiClientService>();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            throw;
        }
        builder.Services.RegisterMauiServices(builder.Configuration);
#if WINDOWS
        builder.Services.AddScoped<IVideoEditor, WindowsVideoEditor>();
#elif IOS
        builder.Services.AddScoped<IVideoEditor, IOSVideoEditor>();
        builder.Services.AddScoped<VideoPickerService>();
#endif

        // Register pages
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<SessionLibrary>();
        builder.Services.AddTransient<SessionPage>();
        builder.Services.AddTransient<VideoLibrary>();
        builder.Services.AddTransient<VideoPlayerView>();
        builder.Services.AddTransient<CameraView>();
        builder.Services.AddSingleton<AppShell>();

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