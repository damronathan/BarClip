using Azure.Identity;
using Azure.Storage.Blobs;
using BarClip.Core.Repositories;
using BarClip.Core.Services;
using BarClip.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;



namespace BarClip.Core
{
    public static class CoreServiceRegistry
    {
        public static IServiceCollection RegisterCoreServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Database registration
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null
                    );
                    sqlOptions.UseRelationalNulls();
                }));



            // Repository registration
            RegisterRepositories(services);

            // Service registration
            RegisterServices(services);

            RegisterExternalServices(services);

            return services;
        }
        private static void RegisterRepositories(IServiceCollection services)
        {
            services.AddScoped<VideoRepository>();
        }

        private static void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<VideoService>();
            services.AddScoped<PlateDetectionService>();
            services.AddScoped<StorageService>();
            services.AddScoped<TrimService>();
            services.AddScoped<FrameService>();
        }
        private static void RegisterExternalServices(IServiceCollection services)
        {
            services.AddSingleton(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var connectionString = configuration.GetConnectionString("AzureStorage");
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Azure Storage connection string is not configured.");
                }

                return new BlobServiceClient(connectionString);
            });
        }


    }
}
