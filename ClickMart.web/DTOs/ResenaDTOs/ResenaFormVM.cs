using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ClickMart.web.DTOs.ResenaDTOs
{
    public class ResenaFormVM
    {
        public int? ResenaId { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        // Solo display
        public string? UsuarioNombre { get; set; }
        public string? UsuarioEmail { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Required, Range(1, 5)]
        public int Calificacion { get; set; }

        [MaxLength(1000)]
        public string? Comentario { get; set; }

        public DateTime? FechaResena { get; set; }

        public IEnumerable<SelectListItem> Usuarios { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> Productos { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
