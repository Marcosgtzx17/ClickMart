namespace ClickMart.Entidades
{
    public class Rol
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}
