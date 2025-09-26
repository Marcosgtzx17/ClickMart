namespace ClickMart.web.DTOs.ProductoDTOs
{
    // Proyección liviana para combos en UI
    public class ProductoLiteDTO
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
    }
}
