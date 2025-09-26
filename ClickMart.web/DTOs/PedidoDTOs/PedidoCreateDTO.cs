namespace ClickMart.web.DTOs.PedidoDTOs
{
    public class PedidoCreateDTO
    {
        public int UsuarioId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;

        public MetodoPagoDTO MetodoPago { get; set; } = MetodoPagoDTO.EFECTIVO;
        public EstadoPagoDTO PagoEstado { get; set; } = EstadoPagoDTO.PENDIENTE;

        // Requerido sólo si MetodoPago = TARJETA
        public string? NumeroTarjeta { get; set; }
    }
}
