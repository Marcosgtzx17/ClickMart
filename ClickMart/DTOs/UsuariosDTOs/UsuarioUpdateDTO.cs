namespace ClickMart.DTOs.UsuariosDTOs
{
    public class UsuarioUpdateDTO
    {
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RolId { get; set; }
    }
}
