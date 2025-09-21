namespace ClickMart.web.DTOs.UsuarioDTOs
{
    public class UsuarioRespuestaDTO
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = "Usuario";
        public string Token { get; set; } = string.Empty;
    }
}