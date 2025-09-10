
using System.ComponentModel.DataAnnotations;

namespace ClickMart.DTOs.ResenaDTOs
{
    public class ResenaUpdateDTO
    {
       
        [Range(1, 5)]
        public byte Calificacion { get; set; }

        [MaxLength(1000)]
        public string? Comentario { get; set; }
    }
}
