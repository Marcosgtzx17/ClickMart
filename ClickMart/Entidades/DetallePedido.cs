using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClickMart.Entidades
{
    [Table("detalle_pedidos")]
    public class DetallePedido
    {
        [Key]
        [Column("DETALLE_ID")]
        public int DetalleId { get; set; }

        // FK a Pedido
        [Required]
        [Column("ID_PEDIDO")]
        public int IdPedido { get; set; }
        [ForeignKey("IdPedido")]
        public Pedido Pedido { get; set; } = null!;

        // FK a Producto
        [Required]
        [Column("ID_PRODUCTO")]
        public int IdProducto { get; set; }
        [ForeignKey("IdProducto")]
        public Productos Producto { get; set; } = null!;


        [Required]
        [Column("CANTIDAD")]
        public int Cantidad { get; set; }

        [Column("SUBTOTAL", TypeName = "decimal(18,2)")]
        public decimal? Subtotal { get; set; }
    }

}

