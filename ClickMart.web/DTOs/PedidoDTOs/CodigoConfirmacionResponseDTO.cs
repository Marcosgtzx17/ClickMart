namespace ClickMart.web.DTOs.PedidoDTOs
{
    public class CodigoConfirmacionResponseDTO
    {
        public int IdCodigo { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public DateTime FechaGeneracion { get; set; }
    }
}
