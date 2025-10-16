using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClickMart.Repositorios
{
    public class CategoriaProductoRepository : ICategoriaProductoRepository
    {
        private readonly AppDbContext _ctx;
        public CategoriaProductoRepository(AppDbContext ctx) => _ctx = ctx;

        // === Validación y normalización ===
        private static void Validar(CategoriaProducto entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));

            if (string.IsNullOrWhiteSpace(entity.Nombre))
                throw new ArgumentException("El nombre de la categoría es obligatorio");

            entity.Nombre = entity.Nombre.Trim();
        }

        public async Task<List<CategoriaProducto>> GetAllAsync()
            => await _ctx.Set<CategoriaProducto>()
                         .AsNoTracking()
                         .ToListAsync();

        public Task<CategoriaProducto?> GetByIdAsync(int id)
            => _ctx.Set<CategoriaProducto>().FindAsync(id).AsTask();

        public async Task<CategoriaProducto> AddAsync(CategoriaProducto entity)
        {
            Validar(entity);

            _ctx.Set<CategoriaProducto>().Add(entity);
            await _ctx.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(CategoriaProducto entity)
        {
            Validar(entity);

            _ctx.Set<CategoriaProducto>().Update(entity);
            return await _ctx.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _ctx.Set<CategoriaProducto>().FindAsync(id);
            if (existing is null) return false;

            // ❌ No eliminar si hay productos que referencian esta categoría (HU-1013)
            // Cambia "CategoriaId" si tu FK tiene otro nombre (p.ej., IdCategoria).
            bool enUso = await _ctx.Set<Productos>().AnyAsync(p => p.CategoriaId == id);
            if (enUso) return false;

            _ctx.Set<CategoriaProducto>().Remove(existing);
            return await _ctx.SaveChangesAsync() > 0;
        }
    }
}