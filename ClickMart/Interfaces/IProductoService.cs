using ClickMart.DTOs.ProductoDTOs;

namespace ClickMart.Interfaces
{
    public interface IProductoService
    {
        Task<List<ProductoResponseDTO>> GetAllAsync();
        Task<ProductoResponseDTO?> GetByIdAsync(int id);
        Task<ProductoResponseDTO> CreateAsync(ProductoCreateDTO dto);
        Task<bool> UpdateAsync(int id, ProductoUpdateDTO dto);
        Task<bool> DeleteAsync(int id);

       
        Task<bool> SubirImagenAsync(int idProducto, byte[] bytes);
        Task<byte[]?> ObtenerImagenAsync(int idProducto);
        Task<int> CountByDistribuidorAsync(int distribuidorId);
        Task<int> CountByCategoriaAsync(int categoriaId);

    }
}