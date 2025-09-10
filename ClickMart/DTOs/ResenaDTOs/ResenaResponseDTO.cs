namespace ClickMart.DTOs.ResenaDTOs
{
    public class ResenaResponseDTO
    {
        public int ResenaId { get; set; }
        public int UsuarioId { get; set; }
        public int ProductoId { get; set; }
        public int Calificacion { get; set; }
        public string Comentario { get; set; } = string.Empty;
        public DateTime FechaResena { get; set; }
    }
}