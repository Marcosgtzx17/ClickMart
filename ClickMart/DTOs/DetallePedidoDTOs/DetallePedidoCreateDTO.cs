using System.Text.Json.Serialization;

namespace ClickMart.DTOs.DetallePedidoDTOs
{
    public class DetallePedidoCreateDTO
    {
        public int IdPedido { get; set; }
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        [JsonIgnore] public decimal Subtotal { get; set; }
    }
}
