using System.Net;
using System.Net.Http.Headers;
using ClickMart.web.DTOs.PedidoDTOs;
using ClickMart.web.Helpers;

namespace ClickMart.web.Services
{
    public class PedidoService
    {
        private readonly ApiService _api;
        private readonly IHttpClientFactory _httpFactory; // <-- NUEVO para consumir el PDF vía HttpClient nombrado "Api"
        private const string BASE = "Pedido"; // api/Pedido

        public PedidoService(ApiService api, IHttpClientFactory httpFactory)
        {
            _api = api;
            _httpFactory = httpFactory;
        }

        public Task<List<PedidoResponseDTO>?> GetAllAsync(string token) =>
            _api.GetAsync<List<PedidoResponseDTO>>($"{BASE}", token);

        public Task<List<PedidoResponseDTO>?> GetMineAsync(string token)
            => _api.GetAsync<List<PedidoResponseDTO>>("Pedido/mios", token);

        public Task<PedidoResponseDTO?> GetByIdAsync(int id, string token) =>
            _api.GetAsync<PedidoResponseDTO>($"{BASE}/{id}", token);

        public Task<PedidoResponseDTO?> CreateAsync(PedidoCreateDTO dto, string token) =>
            _api.PostAsync<PedidoCreateDTO, PedidoResponseDTO>($"{BASE}", dto, token);

        public Task<bool> UpdateAsync(PedidoUpdateDTO dto, string token) =>
            _api.PutAsync($"{BASE}/{dto.PedidoId}", dto, token);

        public Task<bool> DeleteAsync(int id, string token) =>
            _api.DeleteAsync($"{BASE}/{id}", token);

        // Acciones auxiliares
        public Task<PedidoTotalResponseDTO?> RecalcularTotalAsync(int id, string token) =>
            _api.PostAsync<object, PedidoTotalResponseDTO>($"{BASE}/{id}/recalcular-total", new { }, token);

        public Task<CodigoConfirmacionResponseDTO?> GenerarCodigoAsync(int id, string token) =>
            _api.PostAsync<object, CodigoConfirmacionResponseDTO>($"{BASE}/{id}/generar-codigo", new { }, token);

        public async Task<bool> ConfirmarPagoAsync(int id, string codigo, string token)
        {
            await _api.PostAsync<CodigoValidarDTO, object>(
                $"Pedido/{id}/confirmar",
                new CodigoValidarDTO { Codigo = codigo },
                token
            );
            return true;
        }

        // =================== NUEVO: traer PDF de factura ===================
        public async Task<byte[]?> GetFacturaPdfAsync(int id, string token)
        {
            // Usa el HttpClient "Api" configurado en Program.cs (BaseAddress = ApiBaseUrl normalizado a /api/)
            var client = _httpFactory.CreateClient("Api");

            using var req = new HttpRequestMessage(HttpMethod.Get, $"{BASE}/{id}/factura");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

            if (resp.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                throw new ApiHttpException(resp.StatusCode, string.IsNullOrWhiteSpace(body) ? resp.ReasonPhrase : body);
            }

            return await resp.Content.ReadAsByteArrayAsync();
        }
    }
}
