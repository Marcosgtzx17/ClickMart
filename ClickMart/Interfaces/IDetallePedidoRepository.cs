using ClickMart.Entidades;


namespace ClickMart.Interfaces
{
    public interface IDetallePedidoRepository
    {
        Task<List<DetallePedido>> GetAllAsync();
        Task<List<DetallePedido>> GetByPedidoAsync(int pedidoId);
        Task<DetallePedido?> GetByIdAsync(int id);
        Task<DetallePedido> AddAsync(DetallePedido entity);
        Task<bool> UpdateAsync(DetallePedido entity);
        Task<bool> DeleteAsync(int id);
    }
}