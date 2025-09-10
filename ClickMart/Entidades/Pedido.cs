using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClickMart.Entidades
{
    [Table("pedidos")]
    public class Pedido
    {
        [Key]
        [Column("PEDIDO_ID")]
        public int PedidoId { get; set; }

        // FK → usuarios(USUARIO_ID)
        [Required]
        [Column("ID_USUARIO")]
        public int UsuarioId { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public Usuario? Usuario { get; set; }

        // En DDL es DOUBLE; en .NET usamos decimal por precisión monetaria
        [Column("TOTAL", TypeName = "decimal(10,2)")]
        public decimal? Total { get; set; }

        [Required] 
        [Column("FECHA", TypeName = "date")]
        public DateTime Fecha { get; set; }

        // ==== Campos de pago embebidos (MVP) ====
        [Required]
        [Column("METODO_PAGO")]
        public MetodoPago MetodoPago { get; set; } = MetodoPago.TARJETA;

        [Required]
        [Column("PAGO_ESTADO")]
        public EstadoPago PagoEstado { get; set; } = EstadoPago.PENDIENTE;

        [Column("TARJETA_ULTIMOS4")]
        [StringLength(4)]
        public string? TarjetaUltimos4 { get; set; }

        [Column("PAGO_FECHA")]
        public DateTime? PagoFecha { get; set; }
    }

    public enum MetodoPago
    {
        EFECTIVO,
        TARJETA,
    }

    public enum EstadoPago
    {
        PENDIENTE,
        PAGADO,
    }
}
