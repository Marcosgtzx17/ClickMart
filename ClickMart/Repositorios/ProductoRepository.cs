using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClickMart.Repositorios
{
    public class ProductoRepository : IProductoRepository
    {
        private readonly AppDbContext _ctx;
        public ProductoRepository(AppDbContext ctx) => _ctx = ctx;

        // ===== Validación + normalización =====
        private static void Validar(Productos e)
        {
            if (e is null) throw new ArgumentNullException(nameof(e));

            // Requeridos HU-019: nombre, precio (>0), stock (>=0), categ., distrib.
            if (string.IsNullOrWhiteSpace(e.Nombre) ||
                e.Precio <= 0 ||
                e.Stock < 0 ||
                e.CategoriaId <= 0 ||
                e.DistribuidorId <= 0)
            {
                throw new ArgumentException("Complete todos los campos obligatorios");
            }

            e.Nombre = e.Nombre.Trim();
            // Marca/Talla si existen
            if (e.Marca != null) e.Marca = e.Marca.Trim();
            if (e.Talla != null) e.Talla = e.Talla.Trim();
        }

        public async Task<List<Productos>> GetAllAsync() =>
            await _ctx.Set<Productos>().AsNoTracking().ToListAsync();

        public async Task<Productos?> GetByIdAsync(int id) =>
            await _ctx.Set<Productos>().AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductoId == id);

        public async Task<Productos> AddAsync(Productos entity)
        {
            Validar(entity);
            _ctx.Set<Productos>().Add(entity);
            await _ctx.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(Productos entity)
        {
            Validar(entity);

            // Evita conflicto de tracking: carga existente y copia valores
            var existente = await _ctx.Set<Productos>().FindAsync(entity.ProductoId);
            if (existente is null) return false;

            _ctx.Entry(existente).CurrentValues.SetValues(entity);
            _ctx.Entry(existente).State = EntityState.Modified;
            return await _ctx.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _ctx.Set<Productos>().FindAsync(id);
            if (existing is null) return false;

            // HU-022/HU-023 Restricciones por dependencias (ej.: reseñas, pedidos)
            // Si tienes entidad DetallePedido/PedidoItem, agrega otro AnyAsync sobre esa tabla.
            bool tieneResenas = await _ctx.Set<Resena>().AnyAsync(r => r.ProductoId == id);
            if (tieneResenas) return false;

            _ctx.Set<Productos>().Remove(existing);
            return await _ctx.SaveChangesAsync() > 0;
        }

        // ===== Imagen (BLOB) =====
        public async Task<bool> UpdateImagenAsync(int idProducto, byte[]? imagen)
        {
            var prod = await _ctx.Set<Productos>().FindAsync(idProducto);
            if (prod is null) return false;
            prod.Imagen = imagen;
            return await _ctx.SaveChangesAsync() > 0;
        }

        public async Task<byte[]?> GetImagenAsync(int idProducto) =>
            await _ctx.Set<Productos>()
                      .AsNoTracking()
                      .Where(p => p.ProductoId == idProducto)
                      .Select(p => p.Imagen)
                      .FirstOrDefaultAsync();

        // Helpers de conteo (útiles para reportes/validaciones externas)
        public Task<int> CountByDistribuidorAsync(int distribuidorId) =>
            _ctx.Set<Productos>().AsNoTracking().CountAsync(p => p.DistribuidorId == distribuidorId);

        public Task<int> CountByCategoriaAsync(int categoriaId) =>
            _ctx.Set<Productos>().AsNoTracking().CountAsync(p => p.CategoriaId == categoriaId);
    }
}