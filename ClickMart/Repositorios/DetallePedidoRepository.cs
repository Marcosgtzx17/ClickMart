using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClickMart.Repositorios
{
    public class DetallePedidoRepository : IDetallePedidoRepository
    {
        private readonly AppDbContext _ctx;
        public DetallePedidoRepository(AppDbContext ctx) => _ctx = ctx;

        public async Task<List<DetallePedido>> GetAllAsync() =>
            await _ctx.DetallePedidos
                      .Include(d => d.Producto)      // navegación para nombre/precio
                      .AsNoTracking()
                      .ToListAsync();

        public async Task<List<DetallePedido>> GetByPedidoAsync(int pedidoId) =>
            await _ctx.DetallePedidos
                      .Where(d => d.IdPedido == pedidoId)
                      .Include(d => d.Producto)
                      .AsNoTracking()
                      .ToListAsync();

        // 👇 Necesario para ownership desde el controller
        public async Task<DetallePedido?> GetByIdAsync(int id) =>
            await _ctx.DetallePedidos
                      .Include(d => d.Producto)
                      .AsNoTracking()
                      .FirstOrDefaultAsync(d => d.DetalleId == id);

        public async Task<DetallePedido> AddAsync(DetallePedido entity)
        {
            // Si no viene Subtotal calculado, lo calculamos con precio de producto
            if (entity.Subtotal <= 0)
            {
                var prod = await _ctx.Productos
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(p => p.ProductoId == entity.IdProducto);
                var precio = prod?.Precio ?? 0m; // soporta decimal?
                entity.Subtotal = precio * entity.Cantidad;
            }

            _ctx.DetallePedidos.Add(entity);
            await _ctx.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(DetallePedido entity)
        {
            // Si no viene Subtotal o viene en cero, recomputarlo
            if (entity.Subtotal <= 0)
            {
                var prod = await _ctx.Productos
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(p => p.ProductoId == entity.IdProducto);
                var precio = prod?.Precio ?? 0m;
                entity.Subtotal = precio * entity.Cantidad;
            }

            // Adjuntamos y marcamos campos modificados
            _ctx.Attach(entity);
            _ctx.Entry(entity).Property(x => x.IdProducto).IsModified = true;
            _ctx.Entry(entity).Property(x => x.Cantidad).IsModified = true;
            _ctx.Entry(entity).Property(x => x.Subtotal).IsModified = true;

            return await _ctx.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var e = await _ctx.DetallePedidos.FindAsync(id);
            if (e is null) return false;

            _ctx.DetallePedidos.Remove(e);
            return await _ctx.SaveChangesAsync() > 0;
        }
    }
}