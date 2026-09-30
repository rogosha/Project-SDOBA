using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Project_SDOBA.Models;

namespace Project_SDOBA.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://192.168.31.167:8080/api/v1/"),
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task<HttpResponseMessage> RegisterAsync(
        RegisterRequest request)
    {
        return await _httpClient.PostAsJsonAsync(
            "auth/register",
            request);
    }

    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "auth/login",
            request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content
            .ReadFromJsonAsync<LoginResponse>();
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

    public async Task<User?> GetUserAsync(uint userId)
    {
        var token = await SecureStorage.GetAsync("access_token");

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                "JWT token отсутствует");

        var url = $"users/{userId}";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            url);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"GET {url} вернул " +
                $"{(int)response.StatusCode} " +
                $"{response.StatusCode}: {responseBody}");
        }

        var user = JsonSerializer.Deserialize<User>(
            responseBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (user == null)
        {
            throw new InvalidOperationException(
                $"Не удалось десериализовать пользователя: {responseBody}");
        }

        return user;
    }

    public async Task<User?> SearchUserAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        var token = await SecureStorage.GetAsync("access_token");

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                "JWT token отсутствует");

        var url =
            $"users/search?username={Uri.EscapeDataString(username)}";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            url);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        var responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (response.StatusCode ==
            System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"GET {url} вернул " +
                $"{(int)response.StatusCode} " +
                $"{response.StatusCode}: {responseBody}");
        }

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            throw new InvalidOperationException(
                "Сервер вернул пустой ответ при поиске пользователя");
        }

        var user = JsonSerializer.Deserialize<User>(
            responseBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (user == null)
        {
            throw new InvalidOperationException(
                $"Не удалось десериализовать пользователя: {responseBody}");
        }

        return user;
    }

    public async Task<Conversation?> CreateConversationAsync(
        List<uint> userIds)
    {
        var token = await SecureStorage.GetAsync("access_token");

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                "JWT token отсутствует");

        var requestBody = new CreateConversationRequest
        {
            UserIds = userIds
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "conversations");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Content = JsonContent.Create(requestBody);

        var response = await _httpClient.SendAsync(request);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"POST conversations вернул " +
                $"{(int)response.StatusCode} " +
                $"{response.StatusCode}: {responseBody}");
        }

        var conversation =
            JsonSerializer.Deserialize<Conversation>(
                responseBody,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (conversation == null)
        {
            throw new InvalidOperationException(
                $"Не удалось десериализовать conversation: {responseBody}");
        }

        return conversation;
    }

    public async Task<List<Message>?> GetMessagesAsync(
        uint conversationId)
    {
        var token = await SecureStorage.GetAsync("access_token");

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                "JWT token отсутствует");

        var url =
            $"conversations/{conversationId}/messages";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            url);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"GET {url} вернул " +
                $"{(int)response.StatusCode} " +
                $"{response.StatusCode}: {responseBody}");
        }

        var messages =
            JsonSerializer.Deserialize<List<Message>>(
                responseBody,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (messages == null)
        {
            throw new InvalidOperationException(
                $"Не удалось десериализовать ответ: {responseBody}");
        }

        return messages;
    }

    public async Task DeleteMessageAsync(
        uint conversationId,
        uint messageId)
    {
        var token = await SecureStorage.GetAsync("access_token");

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                "JWT token отсутствует");

        var url =
            $"conversations/{conversationId}/messages/{messageId}";

        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            url);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"DELETE {url} вернул " +
                $"{(int)response.StatusCode} " +
                $"{response.StatusCode}: {responseBody}");
        }
    }

    public async Task<Conversation?> GetConversationAsync(
        uint conversationId)
    {
        var token = await SecureStorage.GetAsync("access_token");

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                "JWT token отсутствует");

        var url = $"conversations/{conversationId}";

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            url);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"GET {url} вернул " +
                $"{(int)response.StatusCode} " +
                $"{response.StatusCode}: {responseBody}");
        }

        var conversation =
            JsonSerializer.Deserialize<Conversation>(
                responseBody,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

        if (conversation == null)
        {
            throw new InvalidOperationException(
                $"Не удалось десериализовать conversation: {responseBody}");
        }

        return conversation;
    }

    public async Task<Conversation?> AddConversationMemberAsync(
        uint conversationId,
        uint userId)
    {
        var token = await SecureStorage.GetAsync("access_token");

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                "JWT token отсутствует");

        var url =
            $"conversations/{conversationId}/members";

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            url);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Content = JsonContent.Create(
            new AddConversationMemberRequest
            {
                UserId = userId
            });

        var response = await _httpClient.SendAsync(request);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"POST {url} вернул " +
                $"{(int)response.StatusCode} " +
                $"{response.StatusCode}: {responseBody}");
        }

        if (string.IsNullOrWhiteSpace(responseBody))
            return null;

        return JsonSerializer.Deserialize<Conversation>(
            responseBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    public async Task RemoveConversationMemberAsync(
        uint conversationId,
        uint userId)
    {
        var token = await SecureStorage.GetAsync("access_token");

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                "JWT token отсутствует");

        var url =
            $"conversations/{conversationId}/members/{userId}";

        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            url);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"DELETE {url} вернул " +
                $"{(int)response.StatusCode} " +
                $"{response.StatusCode}: {responseBody}");
        }
    }

    public async Task<Conversation?> RenameConversationAsync(
        uint conversationId,
        string name)
    {
        var token = await SecureStorage.GetAsync("access_token");

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                "JWT token отсутствует");

        var url =
            $"conversations/{conversationId}";

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            url);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        request.Content = JsonContent.Create(
            new RenameConversationRequest
            {
                Name = name
            });

        var response = await _httpClient.SendAsync(request);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"PUT {url} вернул " +
                $"{(int)response.StatusCode} " +
                $"{response.StatusCode}: {responseBody}");
        }

        if (string.IsNullOrWhiteSpace(responseBody))
            return null;

        return JsonSerializer.Deserialize<Conversation>(
            responseBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}

public class AddConversationMemberRequest
{
    [JsonPropertyName("user_id")]
    public uint UserId { get; set; }
}

public class RenameConversationRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}