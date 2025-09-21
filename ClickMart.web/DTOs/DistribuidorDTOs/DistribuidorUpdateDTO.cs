using System.ComponentModel.DataAnnotations;

namespace ClickMart.web.DTOs.DistribuidorDTOs
{
    public class DistribuidorUpdateDTO
    {
        [Required]
        public int DistribuidorId { get; set; }

        [Required, StringLength(50)]
        public string Nombre { get; set; } = "";

        [Required, StringLength(100)]
        public string Direccion { get; set; } = "";

        [Required, StringLength(9)]
        public string Telefono { get; set; } = "";

        [Required, EmailAddress, StringLength(50)]
        public string Gmail { get; set; } = "";

        [Required, StringLength(100)]
        public string Descripcion { get; set; } = "";

        [DataType(DataType.Date)]
        public DateTime FechaRegistro { get; set; } = DateTime.Today;
    }
}
