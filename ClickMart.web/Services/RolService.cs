using ClickMart.web.DTOs.RolDTOs;

namespace ClickMart.web.Services
{
    public class RolService
    {
        private readonly ApiService _api;
        private const string Endpoint = "rol";  // <<-- SIN "api/"

        public RolService(ApiService api) => _api = api;

        public async Task<List<RolResponseDTO>> GetAllAsync(string token)
        {
            var data = await _api.GetAsync<List<RolResponseDTO>>(Endpoint, token);
            return data ?? new List<RolResponseDTO>();
        }
    }
}
