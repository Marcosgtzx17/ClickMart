using ClickMart.Entidades;

namespace ClickMart.DTOs.PedidoDTOs
{
    public class PedidoCreateDTO
    {
        public int UsuarioId { get; set; }
        public DateTime Fecha { get; set; }

        public MetodoPago MetodoPago { get; set; } = MetodoPago.TARJETA;
        public EstadoPago PagoEstado { get; set; } = EstadoPago.PENDIENTE;

        // Solo si MetodoPago = TARJETA (se valida con Luhn y se guarda last4)
        public string? NumeroTarjeta { get; set; }
    }
}
