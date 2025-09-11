namespace ClickMart.DTOs.DetallePedidoDTOs
{
    public class DetallePedidoUpdateDTO
    {
        public int IdPedido { get; set; }
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        public decimal Subtotal { get; set; }
    }
}
