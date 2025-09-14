using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClickMart.Entidades
{
    [Table("reseñas")] // sin ñ: más portable
    public class Resena
    {
        [Key]
        [Column("RESENA_ID")]
        public int ResenaId { get; set; }

        [Required]
        [Column("USUARIO_ID")]
        public int UsuarioId { get; set; }

        [Required]
        [Column("PRODUCTO_ID")]
        public int ProductoId { get; set; } // <-- int para alinear con Productos.ProductoId

        [Range(1, 5)]
        [Column("CALIFICACION")]
        public int Calificacion { get; set; } // <-- int para alinear con DTOs

        [Column("COMENTARIO")]
        [MaxLength(1000)]
        public string? Comentario { get; set; }

        [Required]
        [Column("FECHA_RESENA")] // sin ñ
        public DateTime FechaResena { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public virtual Usuario? Usuario { get; set; }

        [ForeignKey(nameof(ProductoId))]
        public virtual Productos? Producto { get; set; }
    }
}
