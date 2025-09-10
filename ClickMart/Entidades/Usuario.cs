using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClickMart.Entidades
{
    [Table("usuarios")]
    public class Usuario
    {
        [Key]
        [Column("USUARIO_ID")]
        public int UsuarioId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("NOMBRE")]
        public string Nombre { get; set; } = "";

        [Required]
        [MaxLength(100)]
        [Column("DIRECCION")]
        public string Direccion { get; set; } = "";

        [Required]
        [MaxLength(9)]
        [Column("TELEFONO")]
        public string Telefono { get; set; } = "";

        [Required]
        [MaxLength(100)]
        [EmailAddress]
        [Column("EMAIL")]
        public string Email { get; set; } = "";

        [Required]
        [MaxLength(255)]
        [Column("PASSWORD_HASH")]
        public string PasswordHash { get; set; } = "";

        // Clave foránea
        [Required]
        [Column("ROL_ID")]
        public int RolId { get; set; }

        [ForeignKey("RolId")]
        public Rol Rol { get; set; } = null!;
    }
}
