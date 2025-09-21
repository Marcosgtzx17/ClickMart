using ClickMart.web.DTOs.UsuarioDTOs;

namespace ClickMart.web.Services
{
    public class AuthService
    {
        private readonly ApiService _api;
        public AuthService(ApiService api) => _api = api;

        public Task<UsuarioRespuestaDTO?> LoginAsync(UsuarioLoginDTO dto) =>
            _api.PostAsync<UsuarioLoginDTO, UsuarioRespuestaDTO>("auth/login", dto);

        // ⬇️ AQUÍ EL FIX: usar "auth/register" (en inglés) en vez de "auth/registrar"
        public Task<UsuarioRespuestaDTO?> RegistrarAsync(UsuarioRegistroDTO dto) =>
            _api.PostAsync<UsuarioRegistroDTO, UsuarioRespuestaDTO>("auth/register", dto);

        // Si además listás usuarios desde el front:
        public Task<List<UsuarioRespuestaDTO>?> GetUsuariosAsync(string token) =>
            _api.GetAsync<List<UsuarioRespuestaDTO>>("user/usuarios", token);
    }
}