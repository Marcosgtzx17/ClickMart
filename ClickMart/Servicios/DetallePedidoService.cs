using ClickMart.DTOs.DetallePedidoDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;

namespace ClickMart.Servicios
{
    public class DetallePedidoService : IDetallePedidoService
    {
        private readonly IDetallePedidoRepository _repo;
        private readonly IPedidoService _pedidoSvc;

        public DetallePedidoService(IDetallePedidoRepository repo, IPedidoService pedidoSvc)
        {
            _repo = repo;
            _pedidoSvc = pedidoSvc;
        }

        public async Task<List<DetallePedidoResponseDTO>> GetAllAsync() =>
            (await _repo.GetAllAsync()).Select(ToDto).ToList();

        public async Task<List<DetallePedidoResponseDTO>> GetByPedidoAsync(int pedidoId) =>
            (await _repo.GetByPedidoAsync(pedidoId)).Select(ToDto).ToList();

        public async Task<DetallePedidoResponseDTO?> GetByIdAsync(int id)
        {
            var e = await _repo.GetByIdAsync(id);
            return e is null ? null : ToDto(e);
        }

        // === CREATE ===
        public async Task<DetallePedidoResponseDTO> CreateAsync(DetallePedidoCreateDTO dto)
        {
            var entity = new DetallePedido
            {
                IdPedido = dto.IdPedido,
                IdProducto = dto.IdProducto,
                Cantidad = dto.Cantidad
            };

            var saved = await _repo.AddAsync(entity);

            // 🔁 Recalcular total del pedido
            await _pedidoSvc.RecalcularTotalAsync(saved.IdPedido);

            return ToDto(saved);
        }

        // === UPDATE ===
        public async Task<bool> UpdateAsync(int id, DetallePedidoUpdateDTO dto)
        {
            var current = await _repo.GetByIdAsync(id);
            if (current is null) return false;

            current.Cantidad = dto.Cantidad;

            var ok = await _repo.UpdateAsync(current);

            // 🔁 Recalcular total del pedido
            await _pedidoSvc.RecalcularTotalAsync(current.IdPedido);

            return ok;
        }

        // === DELETE ===
        public async Task<bool> DeleteAsync(int id)
        {
            var current = await _repo.GetByIdAsync(id);
            if (current is null) return false;

            var pedidoId = current.IdPedido;

            var ok = await _repo.DeleteAsync(id);

            if (ok)
                await _pedidoSvc.RecalcularTotalAsync(pedidoId);

            return ok;
        }

        private static DetallePedidoResponseDTO ToDto(DetallePedido e) => new()
        {
            DetalleId = e.DetalleId,
            IdPedido = e.IdPedido,
            IdProducto = e.IdProducto,
            Cantidad = e.Cantidad,
            ProductoNombre = e.Producto?.Nombre,
            PrecioUnitario = e.Producto?.Precio ?? 0m,
            Subtotal = (e.Producto?.Precio ?? 0m) * e.Cantidad
        };
        public Task<int> CountByProductoAsync(int productoId)
          => _repo.CountByProductoAsync(productoId);
    }
}

