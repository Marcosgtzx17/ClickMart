// Repositorios/DetallePedidoRepository.cs
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
                      .Include(d => d.Producto)  // << CLAVE
                      .AsNoTracking()
                      .ToListAsync();

        public async Task<DetallePedido?> GetByIdAsync(int id) =>
            await _ctx.DetallePedidos
                      .Include(d => d.Producto)  // << CLAVE
                      .AsNoTracking()
                      .FirstOrDefaultAsync(d => d.DetalleId == id);

        public async Task<List<DetallePedido>> GetByPedidoAsync(int pedidoId) =>
            await _ctx.DetallePedidos
                      .Where(d => d.IdPedido == pedidoId)
                      .Include(d => d.Producto)  // << CLAVE
                      .AsNoTracking()
                      .ToListAsync();

        public async Task<DetallePedido> AddAsync(DetallePedido entity)
        {
            _ctx.DetallePedidos.Add(entity);
            await _ctx.SaveChangesAsync();

            // Cargar navegación para devolver precio/nombre en la respuesta
            await _ctx.Entry(entity).Reference(d => d.Producto).LoadAsync();
            return entity;
        }

        public async Task<bool> UpdateAsync(DetallePedido entity)
        {
            // 🚫 Evita adjuntar el gráfico completo (Producto). Solo el root.
            entity.Producto = null;

            _ctx.DetallePedidos.Attach(entity);
            // Marca únicamente las columnas que sí cambian
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
