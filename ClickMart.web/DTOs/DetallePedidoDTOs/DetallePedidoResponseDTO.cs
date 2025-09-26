namespace ClickMart.web.DTOs.DetallePedidoDTOs
{
    public class DetallePedidoResponseDTO
    {
        public int DetalleId { get; set; }
        public int IdPedido { get; set; }
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }

        public string? ProductoNombre { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}
