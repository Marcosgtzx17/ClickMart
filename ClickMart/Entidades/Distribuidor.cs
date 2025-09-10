using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClickMart.Entidades
{
    [Table("distribuidores")]
    public class Distribuidor
    {
        public Distribuidor()
        {
            Productos = new HashSet<Productos>();
        }

        // ====== Clave primaria ======
        [Key]
        [Column("DISTRIBUIDOR_ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DistribuidorId { get; set; }

        // ====== Campos obligatorios ======
        [Required]
        [StringLength(50)]
        [Column("NOMBRE")]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Column("DIRECCION")]
        public string Direccion { get; set; } = string.Empty;

        [Required]
        [StringLength(9)]
        [Column("TELEFONO")]
        public string Telefono { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Column("GMAIL")]
        public string Gmail { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Column("DESCRIPCION")]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [Column("FECHA_REGISTRO", TypeName = "date")]
        public DateTime FechaRegistro { get; set; }

        // ====== Relación 1:N con Productos ======
        public virtual ICollection<Productos> Productos { get; set; }

        public override string ToString()
            => $"{DistribuidorId} - {Nombre} ({Telefono})";
    }
}
