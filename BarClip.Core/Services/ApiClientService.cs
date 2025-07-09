using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Net.Http;
using BarClip.Models.Requests;
using System.Text.Json;
using System.Text;

public interface IApiClientService
{
    Task<HttpResponseMessage> SaveVideosAsync(SaveVideosRequest request);
}

public class ApiClientService : IApiClientService
{
    private readonly IConfidentialClientApplication _app;
    private readonly string[] _scopes;
    private readonly HttpClient _httpClient;
    private readonly string _url;

    public ApiClientService(IConfiguration configuration, HttpClient httpClient)
    {
        var clientId = configuration["ClientId"];
        var tenantId = configuration["TenantId"];
        var clientSecret = configuration["ClientSecret"];
        var scopes = configuration["Scopes"];
        var url = configuration["BarClipApiUrl"];

        _scopes = new[] { scopes };

        _app = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(new Uri($"https://BarClip.ciamlogin.com/BarClip.onmicrosoft.com/"))
            .Build();

        _httpClient = httpClient;

        _url = url;
    }

    public async Task<HttpResponseMessage> SaveVideosAsync(SaveVideosRequest request)
    {
        var token = await GetAccessTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_url + "/video/save-videos", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine("API error details:");
            Console.WriteLine(errorJson);

            throw new HttpRequestException($"Request failed with status {response.StatusCode}: {errorJson}");
        }

        return response;



        return response;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var result = await _app.AcquireTokenForClient(_scopes).ExecuteAsync();
        return result.AccessToken;
    }
}
