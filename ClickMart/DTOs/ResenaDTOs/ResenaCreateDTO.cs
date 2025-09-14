using System.ComponentModel.DataAnnotations;

namespace ClickMart.DTOs.ResenaDTOs
{
    public class ResenaCreateDTO
    {
        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Required, Range(1, 5)]
        public int Calificacion { get; set; }

        [MaxLength(1000)]
        public string? Comentario { get; set; }

        // Opcional: si no lo envías, se pone UtcNow en el servicio
        public DateTime? FechaResena { get; set; }
    }
}