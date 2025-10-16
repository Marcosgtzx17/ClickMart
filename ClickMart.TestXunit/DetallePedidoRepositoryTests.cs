using ClickMart.Entidades;
using ClickMart.Repositorios;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClickMart.TestXunit
{
    public class DetallePedidoRepositoryTests
    {
        private AppDbContext Ctx()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"ClickMart_Det_{Guid.NewGuid()}")
                .Options;

            var ctx = new AppDbContext(opts);

            // Seed Productos
            ctx.Set<Productos>().AddRange(
                new Productos { ProductoId = 1, Nombre = "Mouse", Precio = 20m, Stock = 100, CategoriaId = 1, DistribuidorId = 1 },
                new Productos { ProductoId = 2, Nombre = "Teclado", Precio = 30m, Stock = 100, CategoriaId = 1, DistribuidorId = 1 }
            );

            // Seed Pedido
            ctx.Set<Pedido>().Add(new Pedido
            {
                PedidoId = 1,
                UsuarioId = 10,
                Fecha = DateTime.UtcNow,
                Total = 0m,
                MetodoPago = MetodoPago.TARJETA,
                PagoEstado = EstadoPago.PENDIENTE
            });

            // Seed un detalle inicial: 1 x Mouse (20)
            ctx.Set<DetallePedido>().Add(new DetallePedido
            {
                IdPedido = 1,
                IdProducto = 1,
                Cantidad = 1,
                Subtotal = 20m
            });

            ctx.SaveChanges();

            // <<< Recalcular TOTAL del pedido seed para arrancar en 20 >>>
            var totalSeed = ctx.Set<DetallePedido>()
                               .Where(d => d.IdPedido == 1)
                               .Sum(d => d.Subtotal);
            var pedSeed = ctx.Set<Pedido>().First(p => p.PedidoId == 1);
            pedSeed.Total = totalSeed;
            ctx.SaveChanges();

            return ctx;
        }

        // HU-1057: Agregar ítem exitosamente
        [Fact]
        public async Task AgregarItem_actualizaSubtotal_yTotalPedido()
        {
            var ctx = Ctx();
            var repo = new DetallePedidoRepository(ctx);

            var before = await ctx.Set<Pedido>().FindAsync(1);
            Assert.Equal(20m, before!.Total ?? 0m); // por seed

            // Agregamos 2 x Teclado (2 * 30 = 60)
            var nuevo = new DetallePedido { IdPedido = 1, IdProducto = 2, Cantidad = 2 };
            var saved = await repo.AddAsync(nuevo);

            Assert.True(saved.DetalleId > 0);
            Assert.Equal(60m, saved.Subtotal);

            var pedido = await ctx.Set<Pedido>().FindAsync(1);
            Assert.Equal(20m + 60m, pedido!.Total); // 80
        }

        // HU-1058: Validación de cantidad inválida al agregar
        [Fact]
        public async Task AgregarItem_cantidadInvalida_lanzaArgumentException()
        {
            var ctx = Ctx();
            var repo = new DetallePedidoRepository(ctx);

            var invalido = new DetallePedido { IdPedido = 1, IdProducto = 1, Cantidad = 0 };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => repo.AddAsync(invalido));
            Assert.Contains("Cantidad no válida", ex.Message);
        }

        // HU-1059/1060: Ver detalles del pedido (cliente/admin)
        [Fact]
        public async Task VerDetalles_retornaItems_conProducto_ySubtotalCorrecto()
        {
            var ctx = Ctx();
            var repo = new DetallePedidoRepository(ctx);

            var items = await repo.GetByPedidoAsync(1);

            Assert.NotEmpty(items);
            Assert.All(items, it => Assert.NotNull(it.Producto));
            Assert.Contains(items, it =>
                it.Producto!.Nombre == "Mouse" &&
                it.Subtotal == it.Cantidad * it.Producto.Precio);
        }

        // HU-1061: Edición exitosa de ítem
        [Fact]
        public async Task EditarItem_exitoso_recalculaSubtotal_yTotal()
        {
            var ctx = Ctx();
            var repo = new DetallePedidoRepository(ctx);

            // Tomamos el Id real del detalle seed
            var detalleId = ctx.Set<DetallePedido>()
                               .Where(d => d.IdPedido == 1)
                               .Select(d => d.DetalleId)
                               .First();

            // Cambiamos a Teclado x3 (3 * 30 = 90)
            var upd = new DetallePedido { DetalleId = detalleId, IdPedido = 1, IdProducto = 2, Cantidad = 3 };
            var ok = await repo.UpdateAsync(upd);
            Assert.True(ok);

            var d = await ctx.Set<DetallePedido>().FindAsync(detalleId);
            Assert.Equal(90m, d!.Subtotal);

            var pedido = await ctx.Set<Pedido>().FindAsync(1);
            Assert.Equal(90m, pedido!.Total); // reemplaza el 20 anterior por 90
        }

        // HU-1062: Validación al editar cantidad inválida
        [Fact]
        public async Task EditarItem_cantidadInvalida_lanzaArgumentException()
        {
            var ctx = Ctx();
            var repo = new DetallePedidoRepository(ctx);

            var detalleId = ctx.Set<DetallePedido>()
                               .Where(d => d.IdPedido == 1)
                               .Select(d => d.DetalleId)
                               .First();

            var upd = new DetallePedido { DetalleId = detalleId, IdPedido = 1, IdProducto = 1, Cantidad = 0 };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => repo.UpdateAsync(upd));
            Assert.Contains("Cantidad no válida", ex.Message);
        }

        // HU-1063: Eliminación exitosa de ítem
        [Fact]
        public async Task EliminarItem_actualizaTotalPedido()
        {
            var ctx = Ctx();
            var repo = new DetallePedidoRepository(ctx);

            var pedidoAntes = await ctx.Set<Pedido>().FindAsync(1);
            Assert.True((pedidoAntes!.Total ?? 0m) > 0);

            // Id real del detalle seed
            var detalleId = ctx.Set<DetallePedido>()
                               .Where(d => d.IdPedido == 1)
                               .Select(d => d.DetalleId)
                               .First();

            var ok = await repo.DeleteAsync(detalleId);
            Assert.True(ok);

            var pedidoDespues = await ctx.Set<Pedido>().FindAsync(1);
            Assert.Equal(0m, pedidoDespues!.Total ?? 0m);
        }

        // HU-1064: Recalcular total tras cambio de cantidad (cubierto por Update)
        [Fact]
        public async Task RecalculoTotal_trasUpdate()
        {
            var ctx = Ctx();
            var repo = new DetallePedidoRepository(ctx);

            // Agrego otro ítem para hacer el total > 0 (Teclado x1 = 30)
            await repo.AddAsync(new DetallePedido { IdPedido = 1, IdProducto = 2, Cantidad = 1 }); // total 50

            // Obtener el detalle original y cambiar a 2 x Mouse (40)
            var detalleId = ctx.Set<DetallePedido>()
                               .Where(d => d.IdPedido == 1 && d.IdProducto == 1)
                               .Select(d => d.DetalleId)
                               .First();

            var upd = new DetallePedido { DetalleId = detalleId, IdPedido = 1, IdProducto = 1, Cantidad = 2 };
            var ok = await repo.UpdateAsync(upd);
            Assert.True(ok);

            var pedido = await ctx.Set<Pedido>().FindAsync(1);
            // total = detalle actualizado (2*20=40) + el agregado (30) = 70
            Assert.Equal(70m, pedido!.Total);
        }
    }
}