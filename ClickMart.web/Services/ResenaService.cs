using ClickMart.web.DTOs.ResenaDTOs;

namespace ClickMart.web.Services
{
    public class ResenaService
    {
        private readonly ApiService _api;
        private const string Base = "resena";

        public ResenaService(ApiService api) => _api = api;

        public Task<List<ResenaResponseDTO>?> GetAllAsync(string? token = null) =>
            _api.GetAsync<List<ResenaResponseDTO>>($"{Base}", token);

        public Task<ResenaResponseDTO?> GetByIdAsync(int id, string? token = null) =>
            _api.GetAsync<ResenaResponseDTO>($"{Base}/{id}", token);

        public Task<List<ResenaResponseDTO>?> GetByProductoAsync(int productoId, string? token = null) =>
            _api.GetAsync<List<ResenaResponseDTO>>($"{Base}/producto/{productoId}", token);

        public Task<ResenaResponseDTO?> CreateAsync(ResenaCreateDTO dto, string token) =>
            _api.PostAsync<ResenaCreateDTO, ResenaResponseDTO>($"{Base}", dto, token);

        public Task<bool> UpdateAsync(int id, ResenaUpdateDTO dto, string token) =>
            _api.PutNoContentAsync($"{Base}/{id}", dto, token);

        public Task<bool> DeleteAsync(int id, string token) =>
            _api.DeleteAsync($"{Base}/{id}", token);
    }
}
