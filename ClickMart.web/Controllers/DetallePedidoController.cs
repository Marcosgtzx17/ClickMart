using ClickMart.web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ClickMart.web.DTOs.DetallePedidoDTOs;
using ClickMart.web.Helpers;
using ClickMart.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClickMart.web.Controllers
{
    [Authorize]
    public class DetallePedidoController : Controller
    {
        private readonly DetallePedidoService _svc;

        public DetallePedidoController(DetallePedidoService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var token = ClaimsHelper.GetToken(User);
            var data = await _svc.GetAllAsync(token) ?? new List<DetallePedidoResponseDTO>();
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            var vm = await _svc.GetByIdAsync(id, token);
            return vm is null ? NotFound() : View(vm);
        }

        [HttpGet]
        public IActionResult Create() => View(new DetallePedidoCreateDTO());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DetallePedidoCreateDTO dto)
        {
            var token = ClaimsHelper.GetToken(User);
            var created = await _svc.CreateAsync(dto, token);
            return created is null ? View(dto) : RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            var vm = await _svc.GetByIdAsync(id, token);
            return vm is null ? NotFound() : View(new DetallePedidoUpdateDTO { DetalleId = vm.DetalleId, Cantidad = vm.Cantidad });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DetallePedidoUpdateDTO dto)
        {
            var token = ClaimsHelper.GetToken(User);
            var ok = await _svc.UpdateAsync(dto, token);
            return ok ? RedirectToAction(nameof(Index)) : View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            var vm = await _svc.GetByIdAsync(id, token);
            return vm is null ? NotFound() : View(vm);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var token = ClaimsHelper.GetToken(User);
            var ok = await _svc.DeleteAsync(id, token);
            return RedirectToAction(nameof(Index));
        }
    }
}
