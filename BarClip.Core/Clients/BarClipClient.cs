//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using System.Net.Http;
//using System.Text.Json;
//using BarClip.Models.Requests;
//using Microsoft.Extensions.Configuration;

//namespace BarClip.Core.Clients;

//public class BarClipClient
//{
//    private readonly HttpClient _httpClient;
//    private readonly ILogger<BarClipClient> _logger;

//    public BarClipClient(HttpClient httpClient, ILogger<BarClipClient> logger)
//    {
//        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
//        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//    }

//    public async Task<bool> SaveVideosAsync(SaveVideosRequest request)
//    {
//        try
//        {
//            var json = JsonSerializer.Serialize(request);
//            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
//            var response = await _httpClient.PostAsync("/trimmed-videos/save-videos", content);
            
//            if (response.IsSuccessStatusCode)
//            {
//                _logger.LogInformation("Videos saved successfully");
//                return true;
//            }
//            else
//            {
//                _logger.LogError("Failed to save videos. Status: {StatusCode}", response.StatusCode);
//                return false;
//            }
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error occurred while saving videos");
//            return false;
//        }
//    }

//    public static class ServiceCollectionExtensions
//    {
//        public static IServiceCollection AddBarClipClient(this IServiceCollection services, string baseAddress)
//        {
//            services.AddHttpClient<BarClipClient>(client =>
//            {
//                client.BaseAddress = new Uri(baseAddress);
//                client.DefaultRequestHeaders.Add("User-Agent", "BarClip-Client/1.0");
//            });

//            return services;
//        }

//        public static IServiceCollection AddBarClipClient(this IServiceCollection services, Func<IConfiguration, string> baseAddressFactory)
//        {
//            services.AddHttpClient<BarClipClient>((serviceProvider, client) =>
//            {
//                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
//                var baseAddress = baseAddressFactory(configuration);
//                client.BaseAddress = new Uri(baseAddress);
//                client.DefaultRequestHeaders.Add("User-Agent", "BarClip-Client/1.0");
//            });

//            return services;
//        }
//    }
//}
