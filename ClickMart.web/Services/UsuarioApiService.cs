using ClickMart.web.DTOs.UsuarioDTOs;

namespace ClickMart.web.Services
{
    public class UsuarioApiService
    {
        private readonly ApiService _api;
        public UsuarioApiService(ApiService api) => _api = api;

        // Endpoint expuesto por tu API: GET /api/user/usuarios
        public Task<List<UsuarioListadoDTO>?> GetAllAsync(string token) =>
            _api.GetAsync<List<UsuarioListadoDTO>>("user/usuarios", token);
    }
}
