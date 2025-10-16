using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClickMart.Repositorios
{
    public class ResenaRepository : IResenaRepository
    {
        private readonly AppDbContext _ctx;
        public ResenaRepository(AppDbContext ctx) => _ctx = ctx;

        // ===== Helpers =====
        private static void Validar(Resena r)
        {
            if (r is null) throw new ArgumentNullException(nameof(r));
            if (r.UsuarioId <= 0) throw new ArgumentException("Usuario no válido");
            if (r.ProductoId <= 0) throw new ArgumentException("Producto no válido");
            if (string.IsNullOrWhiteSpace(r.Comentario)) throw new ArgumentException("Comentario es requerido");
            if (r.Calificacion < 1 || r.Calificacion > 5) throw new ArgumentException("Calificación debe ser 1..5");

            r.Comentario = r.Comentario.Trim();
            if (r.FechaResena == default) r.FechaResena = DateTime.UtcNow;
        }

        public async Task<List<Resena>> GetAllAsync()
            => await _ctx.Set<Resena>().AsNoTracking().ToListAsync();

        public Task<Resena?> GetByIdAsync(int id)
            => _ctx.Set<Resena>().FindAsync(id).AsTask();

        public async Task<List<Resena>> GetByProductoAsync(int productoId)
            => await _ctx.Set<Resena>()
                         .AsNoTracking()
                         .Where(r => r.ProductoId == productoId)
                         .OrderByDescending(r => r.FechaResena)
                         .ToListAsync();

        public async Task<Resena> AddAsync(Resena entity)
        {
            Validar(entity);
            _ctx.Set<Resena>().Add(entity);
            await _ctx.SaveChangesAsync();
            return entity;
        }

        // Solo permite actualizar si la reseña es del mismo usuario
        public async Task<bool> UpdateAsync(Resena entity)
        {
            Validar(entity);

            var current = await _ctx.Set<Resena>().FindAsync(entity.ResenaId);
            if (current is null) return false;
            if (current.UsuarioId != entity.UsuarioId) return false;

            current.Comentario = entity.Comentario;
            current.Calificacion = entity.Calificacion;
            current.FechaResena = DateTime.UtcNow;

            return await _ctx.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _ctx.Set<Resena>().FindAsync(id);
            if (existing is null) return false;
            _ctx.Set<Resena>().Remove(existing);
            return await _ctx.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteOwnAsync(int id, int usuarioId)
        {
            var existing = await _ctx.Set<Resena>().FindAsync(id);
            if (existing is null) return false;
            if (existing.UsuarioId != usuarioId) return false;

            _ctx.Set<Resena>().Remove(existing);
            return await _ctx.SaveChangesAsync() > 0;
        }

        public async Task<double> GetPromedioByProductoAsync(int productoId)
        {
            var q = _ctx.Set<Resena>()
                        .AsNoTracking()
                        .Where(r => r.ProductoId == productoId)
                        .Select(r => (double)r.Calificacion);

            var count = await q.CountAsync();
            if (count == 0) return 0.0;
            return await q.AverageAsync();
        }

        public Task<int> CountByUsuarioAsync(int usuarioId)
            => _ctx.Set<Resena>().AsNoTracking().CountAsync(r => r.UsuarioId == usuarioId);

        public Task<int> CountByProductoAsync(int productoId)
            => _ctx.Set<Resena>().AsNoTracking().CountAsync(r => r.ProductoId == productoId);
    }
}