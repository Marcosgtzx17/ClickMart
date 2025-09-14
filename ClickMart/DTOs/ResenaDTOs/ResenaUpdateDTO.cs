using System.ComponentModel.DataAnnotations;

namespace ClickMart.DTOs.ResenaDTOs
{
    public class ResenaUpdateDTO
    {
        // Opcional: patch-friendly
        [Range(1, 5)]
        public int? Calificacion { get; set; }

        [MaxLength(1000)]
        public string? Comentario { get; set; }

        public DateTime? FechaResena { get; set; }
    }
}