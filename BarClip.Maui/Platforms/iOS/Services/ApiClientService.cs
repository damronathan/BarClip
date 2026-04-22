using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class ApiClientService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly string _url;

    public ApiClientService(HttpClient httpClient, AuthService authService, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _authService = authService;
        _url = configuration["ApiUrl"];
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _authService.GetTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<HttpResponseMessage> GetAsync(string endpoint)
    {
        await SetAuthHeaderAsync();
        return await _httpClient.GetAsync(_url + endpoint);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T request)
    {
        await SetAuthHeaderAsync();
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.PostAsync(_url + endpoint, content);
    }
    public async Task<string> TestAsync()
    {
        var token = await _authService.GetTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _httpClient.GetAsync(_url + "/api/video/test");
        return await response.Content.ReadAsStringAsync();
    }
}