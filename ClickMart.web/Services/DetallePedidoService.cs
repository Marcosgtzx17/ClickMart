using ClickMart.web.DTOs.DetallePedidoDTOs;

namespace ClickMart.web.Services
{
    public class DetallePedidoService
    {
        private readonly ApiService _api;
        private const string BASE = "DetallePedido"; // api/DetallePedido

        public DetallePedidoService(ApiService api) => _api = api;

        public Task<List<DetallePedidoResponseDTO>?> GetAllAsync(string token) =>
            _api.GetAsync<List<DetallePedidoResponseDTO>>($"{BASE}", token);

        public Task<DetallePedidoResponseDTO?> GetByIdAsync(int id, string token) =>
            _api.GetAsync<DetallePedidoResponseDTO>($"{BASE}/{id}", token);

        public Task<List<DetallePedidoResponseDTO>?> GetByPedidoAsync(int pedidoId, string token) =>
            _api.GetAsync<List<DetallePedidoResponseDTO>>($"{BASE}/pedido/{pedidoId}", token);

        public Task<DetallePedidoResponseDTO?> CreateAsync(DetallePedidoCreateDTO dto, string token) =>
            _api.PostAsync<DetallePedidoCreateDTO, DetallePedidoResponseDTO>($"{BASE}", dto, token);

        public Task<bool> UpdateAsync(DetallePedidoUpdateDTO dto, string token) =>
            _api.PutAsync($"{BASE}/{dto.DetalleId}", dto, token);

        public Task<bool> DeleteAsync(int id, string token) =>
            _api.DeleteAsync($"{BASE}/{id}", token);
    }
}
