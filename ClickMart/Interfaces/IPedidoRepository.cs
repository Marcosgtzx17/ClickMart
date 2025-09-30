using ClickMart.Entidades;


namespace ClickMart.Interfaces
{
    public interface IPedidoRepository
    {
        Task<List<Pedido>> GetAllAsync();
        Task<Pedido?> GetByIdAsync(int id);
        Task<Pedido> AddAsync(Pedido entity);
        Task<bool> UpdateAsync(Pedido entity);
        Task<bool> DeleteAsync(int id);
        Task<List<Pedido>> GetByUsuarioAsync(int usuarioId);
        Task<int> CountByUsuarioAsync(int usuarioId);
    }
}