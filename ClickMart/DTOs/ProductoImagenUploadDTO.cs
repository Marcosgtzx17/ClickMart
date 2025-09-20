using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ClickMart.DTOs.ProductoDTOs
{
    public class ProductoImagenUploadDTO
    {
        [Required]
        public IFormFile Archivo { get; set; } = default!;
    }
}
