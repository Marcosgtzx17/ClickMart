namespace ClickMart.DTOs.FacturaDTOs
{
    public class FacturaDTO
    {
        public int PedidoId { get; set; }
        public DateTime FechaEmision { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public List<FacturaItemDTO> Items { get; set; } = new();
    }
}
