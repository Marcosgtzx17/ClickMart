using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClickMart.Entidades
{
    public class Resena
    {
        [Key]
        public int RESENA_ID { get; set; }

        [Required]
        public int USUARIO_ID { get; set; }

        [Required]
        public int PRODUCTO_ID { get; set; }

        [Range(1, 5)]
        public int calificacion { get; set; }

        [MaxLength(1000)]
        public string comentario { get; set; }

        public DateTime fecha_resena { get; set; }
    }
}
