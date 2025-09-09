using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClickMart.Entidades
{
    [Table("detalle_pedidos")]
    public class DetallePedido
    {
        // ====== Clave primaria ======
        [Key]
        [Column("DETALLE_ID")]
        public int DetalleId { get; set; }

        // ====== Claves foráneas ======
        [Column("ID_PEDIDO")]
        public int? IdPedido { get; set; }

        [Column("ID_PRODUCTO")]
        [StringLength(10)]
        public string? IdProducto { get; set; }

        // ====== Datos obligatorios ======
        [Column("CANTIDAD")]
        [Required]
        public int Cantidad { get; set; }

        [Column("SUBTOTAL")]
        public double? Subtotal { get; set; }

        // ====== Navegaciones ======
        public virtual Pedido? Pedido { get; set; }
        public virtual Producto? Producto { get; set; }

        // Azúcar para debugging / logging
        public override string ToString()
            => $"Detalle #{DetalleId}: Pedido={IdPedido}, Producto={IdProducto}, Cantidad={Cantidad}, Subtotal={Subtotal?.ToString("C") ?? "N/A"}";
    }

    // Placeholders mínimos para navegaciones
    public class Pedido
    {
        public int PedidoId { get; set; }
    }

    public class Productos
    {
        public string ProductoId { get; set; } = null!;
    }
}

