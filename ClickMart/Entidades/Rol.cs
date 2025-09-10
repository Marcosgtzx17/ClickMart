using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClickMart.Entidades
{
    [Table("roles")]
    public class Rol
    {
        [Key]
        [Column("ROL_ID")]
        public int RolId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("NOMBRE")]
        public string Nombre { get; set; } = "";

        // Relación uno a muchos con Usuario
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}
