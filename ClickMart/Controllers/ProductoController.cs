using ClickMart.DTOs.ProductoDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _svc;
        public ProductoController(IProductoService svc) => _svc = svc;

        // CRUD
        [HttpGet]
        public async Task<ActionResult<List<ProductoResponseDTO>>> GetAll() =>
            Ok(await _svc.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductoResponseDTO>> GetById(int id)
        {
            var x = await _svc.GetByIdAsync(id);
            return x is null ? NotFound() : Ok(x);
        }

        [HttpPost]
        public async Task<ActionResult<ProductoResponseDTO>> Create([FromBody] ProductoCreateDTO dto)
        {
            var created = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.ProductoId }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductoUpdateDTO dto)
        {
            var ok = await _svc.UpdateAsync(id, dto);
            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _svc.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }

        // ===== Imagen (BLOB) =====

        // POST /api/producto/{id}/imagen  (multipart/form-data; field: archivo)
        [HttpPost("{id:int}/imagen")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20_000_000)] // 20 MB
        public async Task<IActionResult> SubirImagen(int id, IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { message = "Archivo vacío." });

            using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);
            var ok = await _svc.SubirImagenAsync(id, ms.ToArray());
            return ok ? NoContent() : NotFound();
        }

        // GET /api/producto/{id}/imagen  (devuelve el binario con Content-Type correcto)
        [HttpGet("{id:int}/imagen")]
        [AllowAnonymous] // opcional: hazlo público si quieres mostrar imágenes sin login
        public async Task<IActionResult> ObtenerImagen(int id)
        {
            var bytes = await _svc.ObtenerImagenAsync(id);
            if (bytes == null || bytes.Length == 0) return NotFound();

            var mime = DetectMime(bytes);
            var ext = mime switch
            {
                "image/png" => "png",
                "image/gif" => "gif",
                "image/jpeg" => "jpg",
                _ => "bin"
            };
            var fileName = $"producto_{id}.{ext}";
            return File(bytes, mime, fileName);
        }

        // Helper local para detectar MIME por firma
        private static string DetectMime(byte[] data)
        {
            // PNG
            if (data.Length > 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E &&
                data[3] == 0x47 && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
                return "image/png";

            // JPEG
            if (data.Length > 3 && data[0] == 0xFF && data[1] == 0xD8 && data[^2] == 0xFF && data[^1] == 0xD9)
                return "image/jpeg";

            // GIF
            if (data.Length > 3 && data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
                return "image/gif";

            return "application/octet-stream";
        }
    }
}