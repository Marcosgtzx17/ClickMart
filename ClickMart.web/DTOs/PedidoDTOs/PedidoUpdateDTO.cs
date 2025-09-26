namespace ClickMart.web.DTOs.PedidoDTOs
{
    public class PedidoUpdateDTO
    {
        public int PedidoId { get; set; }
        public DateTime Fecha { get; set; }

        public MetodoPagoDTO MetodoPago { get; set; }
        public EstadoPagoDTO PagoEstado { get; set; }

        // Solo si cambias a TARJETA o actualizas el número
        public string? NumeroTarjeta { get; set; }
    }
}
