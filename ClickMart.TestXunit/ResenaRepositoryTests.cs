using ClickMart.Entidades;
using ClickMart.Repositorios;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClickMart.TestXunit
{
    public class ResenaRepositoryTests
    {
        private AppDbContext NewCtx()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"ClickMart_Resenas_{Guid.NewGuid()}")
                .Options;

            var ctx = new AppDbContext(opts);

            // Productos (mínimos para relación)
            ctx.Set<Productos>().AddRange(
                new Productos { ProductoId = 1, Nombre = "Mouse", Precio = 20m, Stock = 100, CategoriaId = 1, DistribuidorId = 1 },
                new Productos { ProductoId = 2, Nombre = "Teclado", Precio = 30m, Stock = 100, CategoriaId = 1, DistribuidorId = 1 }
            );

            // Reseñas semilla en Producto 1
            ctx.Set<Resena>().AddRange(
                new Resena { ResenaId = 101, ProductoId = 1, UsuarioId = 1, Comentario = "Excelente", Calificacion = 5, FechaResena = DateTime.UtcNow.AddMinutes(-5) },
                new Resena { ResenaId = 102, ProductoId = 1, UsuarioId = 2, Comentario = "Bueno", Calificacion = 3, FechaResena = DateTime.UtcNow.AddMinutes(-3) }
            );

            // Una reseña en Producto 2
            ctx.Set<Resena>().Add(new Resena { ResenaId = 201, ProductoId = 2, UsuarioId = 3, Comentario = "OK", Calificacion = 4, FechaResena = DateTime.UtcNow.AddMinutes(-2) });

            ctx.SaveChanges();
            return ctx;
        }

        // HU-1065: Creación exitosa
        [Fact]
        public async Task HU1065_CrearResena_exitoso_persiste()
        {
            var ctx = NewCtx();
            var repo = new ResenaRepository(ctx);

            var nueva = new Resena
            {
                ProductoId = 1,
                UsuarioId = 4,
                Comentario = "  Muy buena  ",
                Calificacion = 4
            };

            var saved = await repo.AddAsync(nueva);

            Assert.True(saved.ResenaId > 0);
            var enDb = await repo.GetByIdAsync(saved.ResenaId);
            Assert.NotNull(enDb);
            Assert.Equal("Muy buena", enDb!.Comentario); // trim aplicado
            Assert.Equal(4, enDb.Calificacion);
        }

        // HU-1066: Validación de campos obligatorios / rangos
        [Fact]
        public async Task HU1066_CrearResena_invalida_lanzaArgumentException()
        {
            var ctx = NewCtx();
            var repo = new ResenaRepository(ctx);

            // comentario vacío
            var r1 = new Resena { ProductoId = 1, UsuarioId = 1, Comentario = "   ", Calificacion = 5 };
            await Assert.ThrowsAsync<ArgumentException>(() => repo.AddAsync(r1));

            // calificación fuera de rango
            var r2 = new Resena { ProductoId = 1, UsuarioId = 1, Comentario = "ok", Calificacion = 6 };
            await Assert.ThrowsAsync<ArgumentException>(() => repo.AddAsync(r2));
        }

        // HU-1067: Visualización del listado por producto
        [Fact]
        public async Task HU1067_ListadoPorProducto_devuelveResenasDelProducto()
        {
            var ctx = NewCtx();
            var repo = new ResenaRepository(ctx);

            var list = await repo.GetByProductoAsync(1);

            Assert.True(list.Count >= 2);
            Assert.All(list, r => Assert.Equal(1, r.ProductoId));
        }

        // HU-1068: Promedio visible
        [Fact]
        public async Task HU1068_Promedio_porProducto_esCorrecto()
        {
            var ctx = NewCtx();
            var repo = new ResenaRepository(ctx);

            var avg = await repo.GetPromedioByProductoAsync(1); // (5 + 3) / 2 = 4
            Assert.InRange(avg, 3.99, 4.01);
        }

        // HU-1069: Edición exitosa (solo propia)
        [Fact]
        public async Task HU1069_EditarResena_propia_exitoso_y_ajena_falla()
        {
            var ctx = NewCtx();
            var repo = new ResenaRepository(ctx);

            // Propia: UsuarioId = 1 sobre ResenaId = 101
            var ok = await repo.UpdateAsync(new Resena
            {
                ResenaId = 101,
                ProductoId = 1,
                UsuarioId = 1,
                Comentario = "Actualizada",
                Calificacion = 4
            });
            Assert.True(ok);

            var r = await repo.GetByIdAsync(101);
            Assert.Equal("Actualizada", r!.Comentario);
            Assert.Equal(4, r.Calificacion);

            // Ajena: intenta UsuarioId = 2 modificar la 101 (es de 1)
            var fail = await repo.UpdateAsync(new Resena
            {
                ResenaId = 101,
                ProductoId = 1,
                UsuarioId = 2,
                Comentario = "Hack",
                Calificacion = 1
            });
            Assert.False(fail);
        }

        // HU-1070: Eliminación (solo propia)
        [Fact]
        public async Task HU1070_EliminarResena_propia_ok_y_ajena_falla()
        {
            var ctx = NewCtx();
            var repo = new ResenaRepository(ctx);

            // Propia: Usuario 2 elimina su reseña 102
            var ok = await repo.DeleteOwnAsync(102, usuarioId: 2);
            Assert.True(ok);

            var still = await repo.GetByIdAsync(102);
            Assert.Null(still);

            // Ajena: Usuario 2 intenta eliminar reseña 101 (es de 1)
            var no = await repo.DeleteOwnAsync(101, usuarioId: 2);
            Assert.False(no);

            var r101 = await repo.GetByIdAsync(101);
            Assert.NotNull(r101);
        }
    }
}