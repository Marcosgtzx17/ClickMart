namespace ClickMart.web.DTOs.CatalogoDTOs
{
    public class CategoriaDTO
    {
        public int CategoriaId { get; set; }   // si tu API usa "Id", cámbialo por Id
        public string Nombre { get; set; } = string.Empty;
    }
}
