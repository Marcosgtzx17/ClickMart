using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClickMart.Entidades
{
    [Table("productos")]
    public class Producto
    {
        [Key]
        [Column("PRODUCTO_ID")]
        public int ProductoId { get; set; }

        [Required]
        [Column("NOMBRE")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Column("DESCRIPCION")]
        [StringLength(100)]
        public string? Descripcion { get; set; }

        [Column("TALLA")]
        [StringLength(10)]
        public string? Talla { get; set; }

        [Required]
        [Column("PRECIO", TypeName = "decimal(10,2)")]
        public decimal Precio { get; set; }

        [Column("MARCA")]
        [StringLength(30)]
        public string? Marca { get; set; }

        [Column("STOCK")]
        public int? Stock { get; set; }

        // ======= Relaciones =======

        [Column("ID_CATEGORIA")]
        public int? CategoriaId { get; set; }

        [ForeignKey("CategoriaId")]
        public CategoriaProducto? Categoria { get; set; }

        [Column("ID_DISTRIBUIDOR")]
        public int? DistribuidorId { get; set; }

        [ForeignKey("DistribuidorId")]
        public Distribuidor? Distribuidor { get; set; }
    }
}
