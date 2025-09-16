using ClickMart.DTOs.DetallePedidoDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;


namespace ClickMart.Servicios
{
    public class DetallePedidoService : IDetallePedidoService
    {
        private readonly IDetallePedidoRepository _repo;
        public DetallePedidoService(IDetallePedidoRepository repo) => _repo = repo;


        public async Task<List<DetallePedidoResponseDTO>> GetAllAsync() =>
        (await _repo.GetAllAsync()).Select(ToDto).ToList();


        public async Task<List<DetallePedidoResponseDTO>> GetByPedidoAsync(int pedidoId) =>
        (await _repo.GetByPedidoAsync(pedidoId)).Select(ToDto).ToList();


        public async Task<DetallePedidoResponseDTO?> GetByIdAsync(int id)
        {
            var e = await _repo.GetByIdAsync(id);
            return e is null ? null : ToDto(e);
        }


        public async Task<DetallePedidoResponseDTO> CreateAsync(DetallePedidoCreateDTO dto)
        {
            var entity = new DetallePedido
            {
                IdPedido = dto.IdPedido,
                IdProducto = dto.IdProducto,
                Cantidad = dto.Cantidad,
                Subtotal = dto.Subtotal
            };
            var saved = await _repo.AddAsync(entity);
            return ToDto(saved);
        }


        public async Task<bool> UpdateAsync(int id, DetallePedidoUpdateDTO dto)
        {
            var current = await _repo.GetByIdAsync(id);
            if (current is null) return false;
            current.Cantidad = dto.Cantidad;
            current.Subtotal = dto.Subtotal;
            return await _repo.UpdateAsync(current);
        }


        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);


        private static DetallePedidoResponseDTO ToDto(DetallePedido e) => new()
        {
            DetalleId = e.DetalleId,
            IdPedido = e.IdPedido,
            IdProducto = e.IdProducto,
            Cantidad = e.Cantidad,
            Subtotal = e.Subtotal
        };
    }
}