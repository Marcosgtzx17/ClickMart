using ClickMart.web.DTOs.CategoriaDTOs;

namespace ClickMart.web.Services
{
    public class CategoriaService
    {
        private readonly ApiService _api;
        private const string Base = "categoria";

        public CategoriaService(ApiService api) => _api = api;

        // GET /api/categoria
        public Task<List<CategoriaResponseDTO>?> GetAllAsync(string? token = null) =>
            _api.GetAsync<List<CategoriaResponseDTO>>($"{Base}", token);

        // GET /api/categoria/{id}
        public Task<CategoriaResponseDTO?> GetByIdAsync(int id, string? token = null) =>
            _api.GetAsync<CategoriaResponseDTO>($"{Base}/{id}", token);

        // POST /api/categoria
        public Task<CategoriaResponseDTO?> CreateAsync(CategoriaCreateDTO dto, string token) =>
            _api.PostAsync<CategoriaCreateDTO, CategoriaResponseDTO>($"{Base}", dto, token);

        // PUT /api/categoria/{id}
        public async Task<bool> UpdateAsync(int id, CategoriaUpdateDTO dto, string token)
        {
            await _api.PutAsync<CategoriaUpdateDTO, CategoriaResponseDTO>($"{Base}/{id}", dto, token);
            return true;
        }

        // DELETE /api/categoria/{id}
        public Task<bool> DeleteAsync(int id, string token) =>
            _api.DeleteAsync($"{Base}/{id}", token);
    }
}