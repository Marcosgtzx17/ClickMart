// Entidades/CodigoConfirmacion.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClickMart.Entidades
{
    [Table("CodigoConfirmacion")] // nombre EXACTO de tu tabla
    public class CodigoConfirmacion
    {
        [Key]
        [Column("id_codigo")]
        public int IdCodigo { get; set; }

        [Required]
        [Column("Email")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column("Codigo")]
        [StringLength(50)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [Column("fecha_generacion")] // <-- ¡AQUÍ el cambio! La columna en BD se llama 'Fecha'
        public DateTime FechaGeneracion { get; set; }

        [Required]
        [Column("Usado")]
        public int Usado { get; set; } = 0; // 0 = no usado, 1 = usado
    }
}
