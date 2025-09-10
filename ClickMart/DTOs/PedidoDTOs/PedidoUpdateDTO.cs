using ClickMart.Entidades;

namespace ClickMart.DTOs.PedidoDTOs
{
    public class PedidoUpdateDTO
    {
        public DateTime Fecha { get; set; }

        public MetodoPago MetodoPago { get; set; }
        public EstadoPago PagoEstado { get; set; }

        public DateTime? PagoFecha { get; set; }
    }
}
