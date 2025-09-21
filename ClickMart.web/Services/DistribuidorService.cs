using ClickMart.web.DTOs.DistribuidorDTOs;

namespace ClickMart.web.Services
{
    public class DistribuidorService
    {
        private readonly ApiService _api;
        public DistribuidorService(ApiService api) => _api = api;

        // Endpoints de la API (case-insensitive):
        private const string BASE = "Distribuidor"; // api/Distribuidor

        public Task<List<DistribuidorResponseDTO>?> GetAllAsync(string token) =>
            _api.GetAsync<List<DistribuidorResponseDTO>>($"{BASE}", token);

        public Task<DistribuidorResponseDTO?> GetByIdAsync(int id, string token) =>
            _api.GetAsync<DistribuidorResponseDTO>($"{BASE}/{id}", token);

        public Task<DistribuidorResponseDTO?> CreateAsync(DistribuidorCreateDTO dto, string token) =>
            _api.PostAsync<DistribuidorCreateDTO, DistribuidorResponseDTO>($"{BASE}", dto, token);

        public Task<bool> UpdateAsync(DistribuidorUpdateDTO dto, string token) =>
            _api.PutAsync($"{BASE}/{dto.DistribuidorId}", dto, token);

        public Task<bool> DeleteAsync(int id, string token) =>
            _api.DeleteAsync($"{BASE}/{id}", token);
    }
}
