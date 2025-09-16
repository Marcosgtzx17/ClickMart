namespace ClickMart.DTOs.ProductoDTOs
{
    public class ProductoUpdateDTO
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Talla { get; set; }
        public decimal Precio { get; set; }
        public string? Marca { get; set; }
        public int? Stock { get; set; }
        public string? ImagenBase64 { get; set; }
        public int? CategoriaId { get; set; }
        public int? DistribuidorId { get; set; }
    }
}
