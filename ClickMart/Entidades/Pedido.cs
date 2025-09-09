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

        [Required]
        [Column("ID_USUARIO")]
        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }

        [Column("TOTAL", TypeName = "decimal(10,2)")]
        public decimal? Total { get; set; }

        [Required]
        [Column("ESTADO")]
        public EstadoPedido Estado { get; set; } = EstadoPedido.CONFIRMADO;

        [Required]
        [Column("FECHA", TypeName = "date")]
        public DateTime Fecha { get; set; }
    }

    public enum EstadoPedido
    {
        BORRADOR,
        CONFIRMADO,
        PAGADO,
        ENVIADO,
        ENTREGADO,
        CANCELADO
    }
}