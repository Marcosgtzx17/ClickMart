using ClickMart.Entidades;
using ClickMart.Repositorios;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace ClickMart.TestXunit
{
    public class PedidoRepositoryTests
    {
        // Crea un contexto en memoria único para cada prueba
        private AppDbContext GetContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new AppDbContext(options);
        }

        // HU-024: Visualización de pedidos (Administrador)
        [Fact]
        public async Task VisualizacionDePedidos_Admin_DeberiaListarTodos()
        {
            using var context = GetContext("PedidosTest_DB1");
            context.Pedidos.AddRange(
                new Pedido { PedidoId = 1, UsuarioId = 10, Total = 50, Fecha = DateTime.Now, MetodoPago = MetodoPago.TARJETA, PagoEstado = EstadoPago.PENDIENTE },
                new Pedido { PedidoId = 2, UsuarioId = 11, Total = 120, Fecha = DateTime.Now, MetodoPago = MetodoPago.EFECTIVO, PagoEstado = EstadoPago.PAGADO }
            );
            await context.SaveChangesAsync();

            var repo = new PedidoRepository(context);

            var result = await repo.GetAllAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        // HU-025: Visualización de pedidos (Cliente autenticado)
        [Fact]
        public async Task VisualizacionDePedidos_ClienteAutenticado_DeberiaVerSoloLosSuyos()
        {
            using var context = GetContext("PedidosTest_DB2");
            context.Pedidos.AddRange(
                new Pedido { PedidoId = 1, UsuarioId = 1, Total = 100, Fecha = DateTime.Now, MetodoPago = MetodoPago.TARJETA, PagoEstado = EstadoPago.PENDIENTE },
                new Pedido { PedidoId = 2, UsuarioId = 2, Total = 200, Fecha = DateTime.Now, MetodoPago = MetodoPago.EFECTIVO, PagoEstado = EstadoPago.PAGADO }
            );
            await context.SaveChangesAsync();

            var repo = new PedidoRepository(context);

            var result = await repo.GetByUsuarioAsync(1);

            Assert.Single(result);
            Assert.Equal(1, result.First().UsuarioId);
        }

        // HU-025: Acceso restringido a otros pedidos
        [Fact]
        public async Task AccesoRestringidoAOtrosPedidos_NoDebeVerDeOtrosUsuarios()
        {
            using var context = GetContext("PedidosTest_DB3");
            context.Pedidos.AddRange(
                new Pedido { PedidoId = 1, UsuarioId = 1, Total = 50, Fecha = DateTime.Now, MetodoPago = MetodoPago.TARJETA },
                new Pedido { PedidoId = 2, UsuarioId = 2, Total = 75, Fecha = DateTime.Now, MetodoPago = MetodoPago.EFECTIVO }
            );
            await context.SaveChangesAsync();

            var repo = new PedidoRepository(context);
            var pedidosUsuario1 = await repo.GetByUsuarioAsync(1);

            Assert.DoesNotContain(pedidosUsuario1, p => p.UsuarioId != 1);
        }
        [Fact]
        public async Task VerDetalle_ClienteAutenticado_IncluyeItemsEstadoTotal()
        {
            using var context = GetContext("PedidosTest_DB_Detalle");
            // crear pedido
            var pedido = new Pedido
            {
                PedidoId = 100,
                UsuarioId = 42,
                Fecha = DateTime.Today,
                MetodoPago = MetodoPago.TARJETA,
                PagoEstado = EstadoPago.PENDIENTE,
                Total = 0m // lo calcularemos a partir de los detalles
            };
            context.Pedidos.Add(pedido);

            // crear productos (opcional, para tener nombre en el detalle)
            var prodA = new Productos { ProductoId = 5, Nombre = "Mouse", Precio = 10m };
            var prodB = new Productos { ProductoId = 6, Nombre = "Teclado", Precio = 15m };
            context.Set<Productos>().AddRange(prodA, prodB);

            // crear detalles (IdProducto, Cantidad, Subtotal)
            var detalle1 = new DetallePedido
            {
                DetalleId = 1,   // si tu entidad tiene otro nombre para PK ajústalo
                IdPedido = pedido.PedidoId,
                IdProducto = 5,
                Cantidad = 2,
                Subtotal = 20m // 2 * 10
            };
            var detalle2 = new DetallePedido
            {
                DetalleId = 2,
                IdPedido = pedido.PedidoId,
                IdProducto = 6,
                Cantidad = 1,
                Subtotal = 15m // 1 * 15
            };
            context.Set<DetallePedido>().AddRange(detalle1, detalle2);

            // Guardar y actualizar total del pedido
            await context.SaveChangesAsync();
            pedido.Total = detalle1.Subtotal + detalle2.Subtotal;
            context.Pedidos.Update(pedido);
            await context.SaveChangesAsync();

            var repo = new PedidoRepository(context);

            // Act: recuperar pedido (como haría el repo/controller)
            var pedidoFromRepo = await repo.GetByIdAsync(pedido.PedidoId);

            // Recuperar detalles asociados (esto normalmente lo hace el servicio o el repo de detalles)
            var detallesFromRepo = await context.Set<DetallePedido>()
                .AsNoTracking()
                .Where(d => d.IdPedido == pedido.PedidoId)
                .ToListAsync();

            // Opcional: mapear nombre de producto usando Productos (simula lo que haría el servicio)
            var productosDict = await context.Set<Productos>()
                .AsNoTracking()
                .Where(p => detallesFromRepo.Select(d => d.IdProducto).Contains(p.ProductoId))
                .ToDictionaryAsync(p => p.ProductoId);

            // Assert: pedido existe y estado correcto
            Assert.NotNull(pedidoFromRepo);
            Assert.Equal(EstadoPago.PENDIENTE, pedidoFromRepo!.PagoEstado);

            // Assert: detalles correctos
            Assert.Equal(2, detallesFromRepo.Count);

            // Validar contenido de cada item y subtotales
            var itemA = detallesFromRepo.Single(d => d.IdProducto == 5);
            var itemB = detallesFromRepo.Single(d => d.IdProducto == 6);

            Assert.Equal(2, itemA.Cantidad);
            Assert.Equal(20m, itemA.Subtotal);
            Assert.Equal("Mouse", productosDict[itemA.IdProducto].Nombre);

            Assert.Equal(1, itemB.Cantidad);
            Assert.Equal(15m, itemB.Subtotal);
            Assert.Equal("Teclado", productosDict[itemB.IdProducto].Nombre);

            // Validar total del pedido coincide con la suma de subtotales
            var sumaSubtotales = detallesFromRepo.Sum(d => d.Subtotal);
            Assert.Equal(sumaSubtotales, pedidoFromRepo.Total);
        }

        // HU-026: Ver detalle (Cliente autenticado)
        [Fact]
        public async Task VerDetalle_ClienteAutenticado_DeberiaObtenerPorId()
        {
            using var context = GetContext("PedidosTest_DB4");
            context.Pedidos.Add(new Pedido
            {
                PedidoId = 1,
                UsuarioId = 1,
                Total = 150,
                Fecha = DateTime.Today,
                MetodoPago = MetodoPago.TARJETA,
                PagoEstado = EstadoPago.PENDIENTE,
                TarjetaUltimos4 = "1234"
            });
            await context.SaveChangesAsync();

            var repo = new PedidoRepository(context);

            var pedido = await repo.GetByIdAsync(1);

            Assert.NotNull(pedido);
            Assert.Equal(1, pedido!.PedidoId);
            Assert.Equal(EstadoPago.PENDIENTE, pedido.PagoEstado);
        }

        // HU-027: Creación de pedido (Cliente autenticado)
        [Fact]
        public async Task CreacionDePedido_ClienteAutenticado_DeberiaCrearCorrectamente()
        {
            using var context = GetContext("PedidosTest_DB5");
            var repo = new PedidoRepository(context);

            var nuevoPedido = new Pedido
            {
                UsuarioId = 1,
                Total = 0,
                Fecha = DateTime.Today,
                MetodoPago = MetodoPago.TARJETA,
                PagoEstado = EstadoPago.PENDIENTE
            };

            var result = await repo.AddAsync(nuevoPedido);

            Assert.NotNull(result);
            Assert.Equal(EstadoPago.PENDIENTE, result.PagoEstado);
        }

        // HU-028: Edición de pedido
        [Fact]
        public async Task EdicionDePedido_DeberiaActualizarCampos()
        {
            using var context = GetContext("PedidosTest_DB6");
            var pedido = new Pedido
            {
                PedidoId = 1,
                UsuarioId = 1,
                Total = 100,
                Fecha = DateTime.Today,
                MetodoPago = MetodoPago.TARJETA,
                PagoEstado = EstadoPago.PENDIENTE
            };
            context.Pedidos.Add(pedido);
            await context.SaveChangesAsync();

            var repo = new PedidoRepository(context);
            pedido.Total = 150;
            pedido.PagoEstado = EstadoPago.PAGADO;
            pedido.TarjetaUltimos4 = "4321";

            var result = await repo.UpdateAsync(pedido);
            var actualizado = await repo.GetByIdAsync(1);

            Assert.True(result);
            Assert.Equal(150, actualizado!.Total);
            Assert.Equal(EstadoPago.PAGADO, actualizado.PagoEstado);
            Assert.Equal("4321", actualizado.TarjetaUltimos4);
        }

        // HU-029: Eliminación de pedido
        [Fact]
        public async Task EliminacionDePedido_DeberiaEliminarCorrectamente()
        {
            using var context = GetContext("PedidosTest_DB7");
            var pedido = new Pedido
            {
                PedidoId = 1,
                UsuarioId = 1,
                Total = 100,
                Fecha = DateTime.Today,
                MetodoPago = MetodoPago.TARJETA,
                PagoEstado = EstadoPago.PENDIENTE
            };
            context.Pedidos.Add(pedido);
            await context.SaveChangesAsync();

            var repo = new PedidoRepository(context);
            var result = await repo.DeleteAsync(1);
            var pedidoEliminado = await repo.GetByIdAsync(1);

            Assert.True(result);
            Assert.Null(pedidoEliminado);
        }
    }
}
