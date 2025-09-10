
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClickMart.Entidades
{
    
    [Table("reseñas")]
    public class Resena
    {
        [Key]
        [Column("RESEÑA_ID")]
        public int ResenaId { get; set; }

        [Required]
        [Column("USUARIO_ID")]
        public int UsuarioId { get; set; }

        
        [Required]
        [MaxLength(10)]
        [Column("PRODUCTO_ID")]
        public string ProductoId { get; set; } = string.Empty;

      
        [Range(1, 5)]
        [Column("calificacion")]
        public byte Calificacion { get; set; }

      
        [Column("comentario", TypeName = "TEXT")]
        public string? Comentario { get; set; }

        [Column("fecha_reseña")]
        public DateTime FechaResena { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public virtual Usuario? Usuario { get; set; }

        [ForeignKey(nameof(ProductoId))]
        public virtual Productos? Producto { get; set; }
    }
}
