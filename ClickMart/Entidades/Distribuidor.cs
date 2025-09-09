using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClickMart.Entidades
{
    [Table("distribuidores")]
    public class Distribuidor
    {
        public Distribuidor()
        {
            // Inicializa la navegación para evitar null refs en tiempo de ejecución.
            Productos = new HashSet<Producto>();
        }

        // ====== Clave primaria ======
        [Key]
        [Column("DISTRIBUIDOR_ID")]
        [Required]
        [StringLength(5)]
        public string DistribuidorId { get; set; } = null!;

        // ====== Campos obligatorios según DDL ======
        [Column("NOMBRE")]
        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = null!;

        [Column("DIRECCION")]
        [Required]
        [StringLength(100)]
        public string Direccion { get; set; } = null!;

        [Column("TELEFONO")]
        [Required]
        [StringLength(9)]
        public string Telefono { get; set; } = null!;

        [Column("GMAIL")]
        [Required]
        [StringLength(50)]
        public string Gmail { get; set; } = null!;

        [Column("DESCRIPCION")]
        [Required]
        [StringLength(100)]
        public string Descripcion { get; set; } = null!;

        [Column("FECHA_REGISTRO", TypeName = "date")]
        [Required]
        public DateTime FechaRegistro { get; set; }

        // ====== Navegación (1:N) con productos ======
        // FK en productos: ID_DISTRIBUIDOR -> DISTRIBUIDOR_ID
        public virtual ICollection<Producto> Productos { get; set; }

        // Opcional: azúcar sintáctico para debugging / logging
        public override string ToString()
            => $"{DistribuidorId} - {Nombre} ({Telefono})";
    }

    // Sombra mínima para la navegación; reemplaza por tu entidad real.
    public class Producto
    {
        // Clave y otras props irían aquí; se define solo para compilar navegaciones.
        public string ProductoId { get; set; } = null!;
        public string? IdDistribuidor { get; set; }
        public virtual Distribuidor? Distribuidor { get; set; }
    }
}
