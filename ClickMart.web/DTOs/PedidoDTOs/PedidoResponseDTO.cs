namespace ClickMart.web.DTOs.PedidoDTOs
{
    public class PedidoResponseDTO
    {
        public int PedidoId { get; set; }
        public int UsuarioId { get; set; }
        public decimal? Total { get; set; }
        public DateTime Fecha { get; set; }

        public MetodoPagoDTO MetodoPago { get; set; }
        public EstadoPagoDTO PagoEstado { get; set; }
        public string? TarjetaUltimos4 { get; set; }
    }
}
