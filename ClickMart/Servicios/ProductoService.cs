using ClickMart.DTOs.ProductoDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;

namespace ClickMart.Servicios
{
    public class ProductoService : IProductoService
    {
        private readonly IProductoRepository _repo;
        public ProductoService(IProductoRepository repo) => _repo = repo;

        public async Task<List<ProductoResponseDTO>> GetAllAsync() =>
            (await _repo.GetAllAsync()).Select(x => new ProductoResponseDTO
            {
                ProductoId = x.ProductoId,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion,
                Talla = x.Talla,
                Precio = x.Precio,
                Marca = x.Marca,
                Stock = x.Stock,
                ImagenAlt = x.ImagenAlt,
                CategoriaId = x.CategoriaId,
                DistribuidorId = x.DistribuidorId
            }).ToList();

        public async Task<ProductoResponseDTO?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            return x is null ? null : new ProductoResponseDTO
            {
                ProductoId = x.ProductoId,
                Nombre = x.Nombre,
                Descripcion = x.Descripcion,
                Talla = x.Talla,
                Precio = x.Precio,
                Marca = x.Marca,
                Stock = x.Stock,
                ImagenAlt = x.ImagenAlt,
                CategoriaId = x.CategoriaId,
                DistribuidorId = x.DistribuidorId
            };
        }

        public async Task<ProductoResponseDTO> CreateAsync(ProductoCreateDTO dto)
        {
            var entity = new Productos
            {
                Nombre = dto.Nombre.Trim(),
                Descripcion = dto.Descripcion,
                Talla = dto.Talla,
                Precio = dto.Precio,
                Marca = dto.Marca,
                Stock = dto.Stock,
                ImagenAlt = dto.ImagenAlt,
                CategoriaId = dto.CategoriaId,
                DistribuidorId = dto.DistribuidorId
            };
            var saved = await _repo.AddAsync(entity);
            return await GetByIdAsync(saved.ProductoId) ?? throw new InvalidOperationException();
        }

        public async Task<bool> UpdateAsync(int id, ProductoUpdateDTO dto)
        {
            var current = await _repo.GetByIdAsync(id);
            if (current is null) return false;

            current.Nombre = dto.Nombre.Trim();
            current.Descripcion = dto.Descripcion;
            current.Talla = dto.Talla;
            current.Precio = dto.Precio;
            current.Marca = dto.Marca;
            current.Stock = dto.Stock;
            current.ImagenAlt = dto.ImagenAlt;
            current.CategoriaId = dto.CategoriaId;
            current.DistribuidorId = dto.DistribuidorId;

            return await _repo.UpdateAsync(current);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);
    }
}