using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClickMart.Entidades
{
    [Table("categoria_productos")]
    public class CategoriaProducto
    {
        [Key]
        [Column("CATEGORIA_ID")]
        public int CategoriaId { get; set; }

        [Required]
        [MaxLength(120)]
        [Column("NOMBRE")]
        public string Nombre { get; set; } = string.Empty;
    }
}
