using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClickMart.Repositorios
{
    public class DetallePedidoRepository : IDetallePedidoRepository
    {
        private readonly AppDbContext _ctx;
        public DetallePedidoRepository(AppDbContext ctx) => _ctx = ctx;

        // === Helpers ===
        private static void ValidarDatosBasicos(DetallePedido e)
        {
            if (e is null) throw new ArgumentNullException(nameof(e));
            if (e.IdPedido <= 0) throw new ArgumentException("Pedido inválido");
            if (e.IdProducto <= 0) throw new ArgumentException("Producto inválido");
            if (e.Cantidad <= 0) throw new ArgumentException("Cantidad no válida");
        }

        private async Task<decimal> PrecioDeProductoAsync(int idProducto)
        {
            var prod = await _ctx.Set<Productos>()
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(p => p.ProductoId == idProducto);
            if (prod is null) throw new ArgumentException("Producto no existe");
            return prod.Precio;
        }

        private async Task RecalcularTotalPedidoAsync(int pedidoId)
        {
            var total = await _ctx.Set<DetallePedido>()
                                  .Where(d => d.IdPedido == pedidoId)
                                  .SumAsync(d => (decimal?)d.Subtotal) ?? 0m;

            var pedido = await _ctx.Set<Pedido>().FindAsync(pedidoId);
            if (pedido != null)
            {
                pedido.Total = total;
                await _ctx.SaveChangesAsync();
            }
        }

        // === Query ===
        public async Task<List<DetallePedido>> GetAllAsync() =>
            await _ctx.DetallePedidos
                      .Include(d => d.Producto)
                      .AsNoTracking()
                      .ToListAsync();

        public async Task<List<DetallePedido>> GetByPedidoAsync(int pedidoId) =>
            await _ctx.DetallePedidos
                      .Where(d => d.IdPedido == pedidoId)
                      .Include(d => d.Producto)
                      .AsNoTracking()
                      .ToListAsync();

        public async Task<DetallePedido?> GetByIdAsync(int id) =>
            await _ctx.DetallePedidos
                      .Include(d => d.Producto)
                      .AsNoTracking()
                      .FirstOrDefaultAsync(d => d.DetalleId == id);

        // === Commands ===
        public async Task<DetallePedido> AddAsync(DetallePedido entity)
        {
            ValidarDatosBasicos(entity);

            var precio = await PrecioDeProductoAsync(entity.IdProducto);
            entity.Subtotal = precio * entity.Cantidad;

            _ctx.DetallePedidos.Add(entity);
            await _ctx.SaveChangesAsync();

            await RecalcularTotalPedidoAsync(entity.IdPedido);
            return entity;
        }

        public async Task<bool> UpdateAsync(DetallePedido entity)
        {
            ValidarDatosBasicos(entity);

            var existente = await _ctx.DetallePedidos.FindAsync(entity.DetalleId);
            if (existente is null) return false;

            existente.IdProducto = entity.IdProducto;
            existente.Cantidad = entity.Cantidad;

            var precio = await PrecioDeProductoAsync(entity.IdProducto);
            existente.Subtotal = precio * entity.Cantidad;

            var ok = await _ctx.SaveChangesAsync() > 0;
            await RecalcularTotalPedidoAsync(existente.IdPedido);
            return ok;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var e = await _ctx.DetallePedidos.FindAsync(id);
            if (e is null) return false;

            var pedidoId = e.IdPedido;
            _ctx.DetallePedidos.Remove(e);
            var ok = await _ctx.SaveChangesAsync() > 0;

            await RecalcularTotalPedidoAsync(pedidoId);
            return ok;
        }

        public Task<int> CountByProductoAsync(int productoId)
          => _ctx.Set<DetallePedido>()
                 .AsNoTracking()
                 .CountAsync(d => d.IdProducto == productoId);
    }
}