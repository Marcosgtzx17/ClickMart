// Controllers/ProductoController.cs
using ClickMart.DTOs.ProductoDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // regla general: requiere auth; GETs públicos abajo con [AllowAnonymous]
    public class ProductoController : ControllerBase
    {
        private readonly IProductoService _svc;
        public ProductoController(IProductoService svc) => _svc = svc;

        // ===== Lectura pública =====

        // GET /api/producto
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<ProductoResponseDTO>>> GetAll() =>
            Ok(await _svc.GetAllAsync());

        // GET /api/producto/{id}
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductoResponseDTO>> GetById(int id)
        {
            var x = await _svc.GetByIdAsync(id);
            return x is null ? NotFound() : Ok(x);
        }

        // GET /api/producto/{id}/imagen
        [HttpGet("{id:int}/imagen")]
        [AllowAnonymous]
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
                "image/webp" => "webp",
                _ => "bin"
            };
            var fileName = $"producto_{id}.{ext}";
            return File(bytes, mime, fileName);
        }

        // ===== Mutaciones (solo admin/alias) =====

        // POST /api/producto
        [HttpPost]
        [Authorize(Roles = "Admin,Administrador,adminitrador,administradores")]
        public async Task<ActionResult<ProductoResponseDTO>> Create([FromBody] ProductoCreateDTO dto)
        {
            var created = await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.ProductoId }, created);
        }

        // PUT /api/producto/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Administrador,adminitrador,administradores")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductoUpdateDTO dto)
        {
            var ok = await _svc.UpdateAsync(id, dto);
            return ok ? NoContent() : NotFound();
        }

        // DELETE /api/producto/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Administrador,adminitrador,administradores")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _svc.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }

        // POST /api/producto/{id}/imagen  (multipart/form-data; field: Archivo)
        [HttpPost("{id:int}/imagen")]
        [Authorize(Roles = "Admin,Administrador,adminitrador,administradores")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20_000_000)] // 20 MB
        public async Task<IActionResult> SubirImagen(int id, [FromForm] ProductoImagenUploadDTO form)
        {
            var archivo = form.Archivo;
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { message = "Archivo vacío." });

            using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);
            var bytes = ms.ToArray();

            // Validación por firma
            var mime = DetectMime(bytes);
            if (mime is not ("image/png" or "image/jpeg" or "image/gif" or "image/webp"))
                return BadRequest(new { message = "Tipo no permitido. Usa PNG, JPG, GIF o WEBP." });

            var ok = await _svc.SubirImagenAsync(id, bytes);
            return ok ? NoContent() : NotFound();
        }

        // ===== Helper local para detectar MIME por firma =====
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
            if (data.Length > 6 && data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
                return "image/gif";

            // WEBP: RIFF....WEBP
            if (data.Length > 12 &&
                data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 && // "RIFF"
                data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)  // "WEBP"
                return "image/webp";

            return "application/octet-stream";
        }
    }
}
