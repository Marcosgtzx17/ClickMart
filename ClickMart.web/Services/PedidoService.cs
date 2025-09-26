using ClickMart.web.DTOs.PedidoDTOs;

namespace ClickMart.web.Services
{
    public class PedidoService
    {
        private readonly ApiService _api;
        private const string BASE = "Pedido"; // api/Pedido

        public PedidoService(ApiService api) => _api = api;

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
    }
}
