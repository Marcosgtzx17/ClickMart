namespace ClickMart.DTOs.FacturaDTOs
{
    public class FacturaItemDTO
    {
        public string Producto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }

        // NUEVO: thumbnail del producto (puede ser null)
        public byte[]? ImagenProducto { get; set; }
    }
}
