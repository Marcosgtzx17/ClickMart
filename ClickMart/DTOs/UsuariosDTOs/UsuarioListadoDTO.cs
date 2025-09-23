namespace ClickMart.DTOs.UsuariosDTOs
{
    public class UsuarioListadoDTO
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string Rol { get; set; } = string.Empty;
    }
}
