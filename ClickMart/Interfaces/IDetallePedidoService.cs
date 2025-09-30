using ClickMart.DTOs.DetallePedidoDTOs;

namespace ClickMart.Interfaces
{
    public interface IDetallePedidoService
    {
        Task<List<DetallePedidoResponseDTO>> GetAllAsync();
        Task<List<DetallePedidoResponseDTO>> GetByPedidoAsync(int pedidoId);
        Task<DetallePedidoResponseDTO?> GetByIdAsync(int id);
        Task<DetallePedidoResponseDTO> CreateAsync(DetallePedidoCreateDTO dto);
        Task<bool> UpdateAsync(int id, DetallePedidoUpdateDTO dto);
        Task<bool> DeleteAsync(int id);
        Task<int> CountByProductoAsync(int productoId);

    }
}
