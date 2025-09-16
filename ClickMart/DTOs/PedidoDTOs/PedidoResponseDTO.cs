using ClickMart.Entidades;

namespace ClickMart.DTOs.PedidoDTOs
{
    public class PedidoResponseDTO
    {
        public int PedidoId { get; set; }
        public int UsuarioId { get; set; }
        public decimal? Total { get; set; }
        public DateTime Fecha { get; set; }
        public MetodoPago MetodoPago { get; set; }
        public EstadoPago PagoEstado { get; set; }
        public string? TarjetaUltimos4 { get; set; }
    }
}
