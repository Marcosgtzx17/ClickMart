using ClickMart.Entidades;

namespace ClickMart.DTOs.PedidoDTOs
{
    public class PedidoCreateDTO
    {
        public int UsuarioId { get; set; }
        public decimal? Total { get; set; }
        public DateTime Fecha { get; set; }

        public MetodoPago MetodoPago { get; set; } = MetodoPago.TARJETA;
        public EstadoPago PagoEstado { get; set; } = EstadoPago.PENDIENTE;

        public string? TarjetaUltimos4 { get; set; }  
        public DateTime? PagoFecha { get; set; }
    }
}
