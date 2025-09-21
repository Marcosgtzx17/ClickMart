namespace ClickMart.web.DTOs.DistribuidorDTOs
{
    public class DistribuidorResponseDTO
    {
        public int DistribuidorId { get; set; }
        public string Nombre { get; set; } = "";
        public string Direccion { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Gmail { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public DateTime FechaRegistro { get; set; }
    }
}
