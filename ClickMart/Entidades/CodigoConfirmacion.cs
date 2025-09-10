using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClickMart.Entidades
{
    [Table("codigos_confirmacion")]
    public class CodigoConfirmacion
    {
        [Key]
        [Column("CODIGO_ID")]
        public int IdCodigo { get; set; }

        [Required]
        [MaxLength(100)]
        [EmailAddress]
        [Column("email")]
        public string Email { get; set; } = "";

        [Required]
        [MaxLength(50)]
        [Column("codigo")]
        public string Codigo { get; set; } = "";

        [Required]
        [Column("fecha_generacion")]
        public DateTime FechaGeneracion { get; set; }

        [Required]
        [Column("usado")]
        public int Usado { get; set; } // 0 = no usado, 1 = usado
    }
}
