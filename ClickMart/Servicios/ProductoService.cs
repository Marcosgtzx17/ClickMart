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
            (await _repo.GetAllAsync()).Select(x => ToDto(x)).ToList();

        public async Task<ProductoResponseDTO?> GetByIdAsync(int id)
        {
            var x = await _repo.GetByIdAsync(id);
            return x is null ? null : ToDto(x);
        }

        public async Task<ProductoResponseDTO> CreateAsync(ProductoCreateDTO dto)
        {
            var entity = new Productos
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Talla = dto.Talla,
                Precio = dto.Precio,
                Marca = dto.Marca,
                Stock = dto.Stock,
                CategoriaId = dto.CategoriaId,
                DistribuidorId = dto.DistribuidorId
            };

            if (!string.IsNullOrWhiteSpace(dto.ImagenBase64))
            {
                try { entity.Imagen = Convert.FromBase64String(dto.ImagenBase64); }
                catch { throw new ArgumentException("ImagenBase64 inválida."); }
            }

            var saved = await _repo.AddAsync(entity);
            return ToDto(saved);
        }

        public async Task<bool> UpdateAsync(int id, ProductoUpdateDTO dto)
        {
            var current = await _repo.GetByIdAsync(id);
            if (current is null) return false;

            current.Nombre = dto.Nombre;
            current.Descripcion = dto.Descripcion;
            current.Talla = dto.Talla;
            current.Precio = dto.Precio;
            current.Marca = dto.Marca;
            current.Stock = dto.Stock;
            current.CategoriaId = dto.CategoriaId;
            current.DistribuidorId = dto.DistribuidorId;

            if (!string.IsNullOrWhiteSpace(dto.ImagenBase64))
            {
                try { current.Imagen = Convert.FromBase64String(dto.ImagenBase64); }
                catch { throw new ArgumentException("ImagenBase64 inválida."); }
            }

            return await _repo.UpdateAsync(current);
        }

        public Task<bool> DeleteAsync(int id) => _repo.DeleteAsync(id);

        public Task<bool> SubirImagenAsync(int idProducto, byte[] bytes) =>
            _repo.UpdateImagenAsync(idProducto, bytes);

        public Task<byte[]?> ObtenerImagenAsync(int idProducto) =>
            _repo.GetImagenAsync(idProducto);

        private static ProductoResponseDTO ToDto(Productos x) => new()
        {
            ProductoId = x.ProductoId,
            Nombre = x.Nombre,
            Descripcion = x.Descripcion,
            Talla = x.Talla,
            Precio = x.Precio,
            Marca = x.Marca,
            Stock = x.Stock,
            CategoriaId = x.CategoriaId,
            DistribuidorId = x.DistribuidorId,
            TieneImagen = x.Imagen != null && x.Imagen.Length > 0
        };
    }
}