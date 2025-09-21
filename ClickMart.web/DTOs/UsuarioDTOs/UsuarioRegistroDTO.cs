using System.Text.Json.Serialization;

namespace ClickMart.web.DTOs.UsuarioDTOs
{
    public class UsuarioRegistroDTO
    {
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // IMPORTANTE: aquí va la contraseña en texto plano (se hashea en el repo)
        public string Password { get; set; } = string.Empty;

        // Si no viene, asignamos un rol por defecto (ej. 2 = "Usuario")
        [JsonIgnore] public int? RolId { get; set; }
    }
}