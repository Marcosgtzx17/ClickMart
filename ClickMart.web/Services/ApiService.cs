using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ClickMart.web.Services
{
    public class ApiService
    {
        private readonly IHttpClientFactory _factory;
        public ApiService(IHttpClientFactory factory) => _factory = factory;

        private HttpClient CreateClient(string? token = null)
        {
            var client = _factory.CreateClient("Api");
            if (!string.IsNullOrWhiteSpace(token))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public async Task<T?> GetAsync<T>(string endpoint, string? token = null)
        {
            var resp = await CreateClient(token).GetAsync(endpoint);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, string? token = null)
        {
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var resp = await CreateClient(token).PostAsync(endpoint, content);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}