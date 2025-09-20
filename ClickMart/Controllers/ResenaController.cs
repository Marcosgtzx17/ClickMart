using ClickMart.DTOs.ResenaDTOs;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class ResenaController : ControllerBase
{
    private readonly IResenaService _svc;
    private readonly IPedidoService _pedidos; // opcional si quieres exigir "producto comprado"
    public ResenaController(IResenaService svc, IPedidoService pedidos)
    {
        _svc = svc;
        _pedidos = pedidos;
    }

    // Helpers
    private int? GetUserId()
    {
        var raw = User.FindFirst("uid")?.Value
                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.Identity?.Name;
        return int.TryParse(raw, out var id) ? id : null;
    }
    private bool IsAdmin() => User.IsInRole("Admin");

    // ===== Lectura =====

    // Público: ver todas o por producto (elige el que uses)
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll() => Ok(await _svc.GetAllAsync());

    [HttpGet("producto/{productoId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByProducto(int productoId)
        => Ok(await _svc.GetByIdAsync(productoId));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var x = await _svc.GetByIdAsync(id);
        return x is null ? NotFound() : Ok(x);
    }

    // ===== Mutaciones =====

    [HttpPost]
    [Authorize(Roles = "Cliente,Admin")]
    public async Task<IActionResult> Create([FromBody] ResenaCreateDTO dto)
    {
        if (!IsAdmin())
        {
            var uid = GetUserId();
            if (uid is null) return Unauthorized();

            // (Opcional) Validar que el usuario compró ese producto antes
            // var compro = await _pedidos.UsuarioComproProductoAsync(uid.Value, dto.ProductoId);
            // if (!compro) return BadRequest(new { message = "Debe haber comprado el producto para reseñarlo." });

            dto.UsuarioId = uid.Value; // forzar ownership
        }

        var created = await _svc.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.ResenaId }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Cliente,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] ResenaUpdateDTO dto)
    {
        var current = await _svc.GetByIdAsync(id);
        if (current is null) return NotFound();

        if (!IsAdmin())
        {
            var uid = GetUserId();
            if (uid is null) return Unauthorized();
            if (current.UsuarioId != uid.Value) return Forbid();
        }

        var ok = await _svc.UpdateAsync(id, dto);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Cliente,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var current = await _svc.GetByIdAsync(id);
        if (current is null) return NotFound();

        if (!IsAdmin())
        {
            var uid = GetUserId();
            if (uid is null) return Unauthorized();
            if (current.UsuarioId != uid.Value) return Forbid();
        }

        var ok = await _svc.DeleteAsync(id);
        return ok ? NoContent() : NotFound();
    }
}
