using ClickMart.DTOs.PedidoDTOs;


namespace ClickMart.Interfaces
{
    public interface IPedidoService
    {
        Task<List<PedidoResponseDTO>> GetAllAsync();
        Task<PedidoResponseDTO?> GetByIdAsync(int id);
        Task<PedidoResponseDTO> CreateAsync(PedidoCreateDTO dto);
        Task<bool> UpdateAsync(int id, PedidoUpdateDTO dto);
        Task<bool> DeleteAsync(int id);
        //Task<bool> RecalcularTotalAsync(int pedidoId);
        // 🔄 Recalcular total del pedido y persistirlo
        Task<decimal> RecalcularTotalAsync(int pedidoId);
        Task<bool> MarcarPagadoAsync(int pedidoId);

        Task<List<PedidoResponseDTO>> GetByUsuarioAsync(int usuarioId);
    }
}