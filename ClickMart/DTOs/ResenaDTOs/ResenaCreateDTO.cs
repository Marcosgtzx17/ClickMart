using System.ComponentModel.DataAnnotations;

namespace ClickMart.DTOs.ResenaDTOs
{
    public class ResenaCreateDTO
    {
        [Required]
        public int UsuarioId { get; set; }

        
        [Required, MaxLength(10)]
        public string ProductoId { get; set; } = string.Empty;

        
        [Range(1, 5)]
        public byte Calificacion { get; set; }

        
        [MaxLength(1000)]
        public string? Comentario { get; set; }
    }
}
