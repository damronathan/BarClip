using BarClip.Models.Requests;
using BarClip.Models.Responses;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
        var response = await _httpClient.GetAsync(_url + "/video/test");
        return await response.Content.ReadAsStringAsync();
    }
    public async Task<UploadSasUrlResponse> GetUploadSasUrlAsync(SasUrlRequest request)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        query["Id"] = request.Id.ToString();
        query["ContainerName"] = request.ContainerName;
        query["Extension"] = request.Extension;

        var url = $"{_url}/video/upload-sas-url?{query}";

        var token = await _authService.GetTokenAsync();

        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(httpRequest);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UploadSasUrlResponse>()
               ?? throw new Exception("Failed to deserialize response");
    }
    public async Task<ICollection<VideoResponse>> GetVideosAsync(GetVideosRequest request)
    {
        var token = await _authService.GetTokenAsync();
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, BuildGetVideosUrl(request));
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ICollection<VideoResponse>>()
               ?? throw new Exception("Failed to deserialize response");
    }

    private string BuildGetVideosUrl(GetVideosRequest request)
    {
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        if (request.SessionId.HasValue)
            query["SessionId"] = request.SessionId.Value.ToString();
        if (request.UserId.HasValue)
            query["UserId"] = request.UserId.Value.ToString();
        return $"{_url}/video?{query}";
    }
}