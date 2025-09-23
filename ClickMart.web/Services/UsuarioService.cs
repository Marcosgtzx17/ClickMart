using ClickMart.web.DTOs.UsuarioDTOs;

namespace ClickMart.web.Services
{
    public class UsuarioService
    {
        private readonly ApiService _api;
        private const string Base = "user";

        public UsuarioService(ApiService api) => _api = api;

        public Task<List<UsuarioListadoDTO>?> GetAllAsync(string token) =>
            _api.GetAsync<List<UsuarioListadoDTO>>($"{Base}/usuarios", token);

        public Task<UsuarioListadoDTO?> GetByIdAsync(int id, string token) =>
            _api.GetAsync<UsuarioListadoDTO>($"{Base}/{id}", token);

        public Task<UsuarioListadoDTO?> CreateAsync(UsuarioCreateDTO dto, string token) =>
            _api.PostAsync<UsuarioCreateDTO, UsuarioListadoDTO>($"{Base}", dto, token);

    public Task<bool> UpdateAsync(int id, UsuarioUpdateDTO dto, string token) =>
        _api.PutNoContentAsync($"{Base}/{id}", dto, token);

        public Task<bool> DeleteAsync(int id, string token) =>
            _api.DeleteAsync($"{Base}/{id}", token);
    }
}
