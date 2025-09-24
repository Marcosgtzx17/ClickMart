using System.ComponentModel.DataAnnotations;

namespace ClickMart.web.DTOs.ResenaDTOs
{
    public class ResenaUpdateDTO
    {
        [Range(1, 5)]
        public int? Calificacion { get; set; }

        [MaxLength(1000)]
        public string? Comentario { get; set; }

        public DateTime? FechaResena { get; set; }
    }
}