using System.Net.Http.Headers;
using System.Net.Http.Json;
using Project_SDOBA.Models;

namespace Project_SDOBA.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:8080/api/v1/"),
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task<HttpResponseMessage> RegisterAsync(RegisterRequest request)
    {
        return await _httpClient.PostAsJsonAsync(
            "auth/register",
            request);
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "auth/login",
            request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<LoginResponse>();
    }

    public async Task<List<Conversation>?> GetConversationsAsync()
    {
        var token = await SecureStorage.GetAsync("access_token");

        if (string.IsNullOrWhiteSpace(token))
            return null;

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "conversations");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content
            .ReadFromJsonAsync<List<Conversation>>();
    }
}
