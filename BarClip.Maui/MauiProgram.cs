using Microsoft.Extensions.Logging;
using BarClip.Core;
using Microsoft.Extensions.Configuration;
using BarClip.Data;
using Microsoft.EntityFrameworkCore;
using SkiaSharp.Views.Maui.Controls.Hosting;
using BarClip.Core.Interfaces;
using Microsoft.Identity.Client;
using CommunityToolkit.Maui;
using BarClip.Maui.Services;
using BarClip.Maui.Models;




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
        SQLitePCL.Batteries_V2.Init();
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



        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "barclip.db");

        var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:DefaultConnection"] = $"Data Source={dbPath}"
    })
    .Build();

        builder.Configuration.AddConfiguration(configuration);
        builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

        builder.Services.RegisterMauiServices(builder.Configuration);
        builder.Services.AddScoped<SetupService>();

#if WINDOWS
        builder.Services.AddScoped<IVideoEditor, WindowsVideoEditor>();
#elif IOS
        builder.Services.AddScoped<IVideoEditor, IOSVideoEditor>();
        builder.Services.AddScoped<VideoPickerService>();
#endif

        // Register pages
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddTransient<SessionLibraryPage>();
        builder.Services.AddTransient<SessionViewModel>();
        builder.Services.AddTransient<SessionPage>();
        builder.Services.AddTransient<VideoLibraryPage>();
        builder.Services.AddTransient<VideoPlayerPage>();
        builder.Services.AddTransient<CameraPage>();
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<IPublicClientApplication>(sp =>
        {
            var authConfig = builder.Configuration.GetSection("AzureAd");
            return PublicClientApplicationBuilder
                .Create(authConfig["ClientId"])
                .WithAuthority($"https://barclip.ciamlogin.com/barclip.onmicrosoft.com/SignUpSignIn")
                .WithRedirectUri($"msal{authConfig["ClientId"]}://auth")
                .WithIosKeychainSecurityGroup("com.nathandamron.barclip")
                .Build();
        });
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddHttpClient<ApiClientService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(120);
        });
        builder.Services.AddSingleton<ApiClientService>();
        builder.Services.AddScoped<UploadService>();
        builder.Services.AddSingleton<SetupService>();

        // ... rest of your existing pre-build code (RegisterMauiServices, pages, etc.) ...

        var app = builder.Build();   // Sentry native handler is now live

        // AFTER Build() — this is what actually touches the keychain,
        // now that Sentry can catch a native crash if it happens
        try
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<IPublicClientApplication>();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            throw;
        }



        return app;
    }


}