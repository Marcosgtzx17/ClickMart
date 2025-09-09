namespace ClickMart.Entidades
{
    public class Usuario
    {
        public string Id { get; set; }

        public string Nombre { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public int IdRol { get; set; }
        public Rol Rol { get; set; } = null!;
        public object RolId { get; internal set; }
    }
}
