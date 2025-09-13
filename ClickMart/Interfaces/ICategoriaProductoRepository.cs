
using ClickMart.Entidades;

namespace ClickMart.Interfaces
{
    public interface ICategoriaProductoRepository
    {
        Task<List<CategoriaProducto>> GetAllAsync();
        Task<CategoriaProducto?> GetByIdAsync(int id);
        Task<CategoriaProducto> AddAsync(CategoriaProducto entity);
        Task<bool> UpdateAsync(CategoriaProducto entity);
        Task<bool> DeleteAsync(int id);
    }
}
