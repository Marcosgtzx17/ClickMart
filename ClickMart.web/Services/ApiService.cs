using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ClickMart.web.Services
{
    // Excepción custom para propagar status + cuerpo de la API
    public class ApiHttpException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string? ResponseBody { get; }

        public ApiHttpException(HttpStatusCode statusCode, string message, string? responseBody = null)
            : base($"[{(int)statusCode}] {message}")
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }

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

        private static string? ExtractApiErrorMessage(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return null;

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("message", out var msg)) return msg.GetString();
                if (root.TryGetProperty("error", out var err)) return err.GetString();
                if (root.TryGetProperty("title", out var title))
                {
                    var t = title.GetString();
                    if (root.TryGetProperty("detail", out var det))
                        t += $": {det.GetString()}";
                    return t;
                }
                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in errors.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array && prop.Value.GetArrayLength() > 0)
                            return prop.Value[0].GetString();
                    }
                }
            }
            catch { /* ignore parse errors */ }

            return null;
        }

        public async Task<T?> GetAsync<T>(string endpoint, string? token = null)
        {
            var resp = await CreateClient(token).GetAsync(endpoint);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                var msg = ExtractApiErrorMessage(json) ?? resp.ReasonPhrase ?? "Error al llamar a la API (GET)";
                throw new ApiHttpException(resp.StatusCode, msg, json);
            }

            return string.IsNullOrWhiteSpace(json)
                ? default
                : JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, string? token = null)
        {
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var resp = await CreateClient(token).PostAsync(endpoint, content);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                var msg = ExtractApiErrorMessage(json) ?? resp.ReasonPhrase ?? "Error al llamar a la API (POST)";
                throw new ApiHttpException(resp.StatusCode, msg, json);
            }

            return string.IsNullOrWhiteSpace(json)
                ? default
                : JsonSerializer.Deserialize<TResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data, string? token = null)
        {
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var resp = await CreateClient(token).PutAsync(endpoint, content);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                var msg = ExtractApiErrorMessage(json) ?? resp.ReasonPhrase ?? "Error al llamar a la API (PUT)";
                throw new ApiHttpException(resp.StatusCode, msg, json);
            }

            return string.IsNullOrWhiteSpace(json)
                ? default
                : JsonSerializer.Deserialize<TResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<bool> DeleteAsync(string endpoint, string? token = null)
        {
            var resp = await CreateClient(token).DeleteAsync(endpoint);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                var msg = ExtractApiErrorMessage(json) ?? resp.ReasonPhrase ?? "Error al llamar a la API (DELETE)";
                throw new ApiHttpException(resp.StatusCode, msg, json);
            }

            return true;
        }

        // Para subir imágenes: multipart/form-data (campo: "archivo")
        public async Task<bool> PostMultipartAsync(string endpoint, MultipartFormDataContent content, string? token = null)
        {
            var resp = await CreateClient(token).PostAsync(endpoint, content);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                var msg = ExtractApiErrorMessage(json) ?? resp.ReasonPhrase ?? "Error al llamar a la API (POST multipart)";
                throw new ApiHttpException(resp.StatusCode, msg, json);
            }

            return true;
        }
       
        public async Task<bool> PutAsync<TRequest>(string endpoint, TRequest data, string? token = null)
        {
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var resp = await CreateClient(token).PutAsync(endpoint, content);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> PutNoContentAsync<TRequest>(string endpoint, TRequest data, string? token = null)
        {
            endpoint = endpoint.TrimStart('/');
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var resp = await CreateClient(token).PutAsync(endpoint, content);
            var json = await resp.Content.ReadAsStringAsync(); // por si viene ProblemDetails en error

            if (!resp.IsSuccessStatusCode)
            {
                var msg = ExtractApiErrorMessage(json) ?? resp.ReasonPhrase ?? "Error al llamar a la API (PUT)";
                throw new ApiHttpException(resp.StatusCode, msg, json);
            }
            return true;
        }
        //public async Task<bool> DeleteAsync(string endpoint, string? token = null)
        // {
        //   var resp = await CreateClient(token).DeleteAsync(endpoint);
        //  return resp.IsSuccessStatusCode;
        // }
    }
}
