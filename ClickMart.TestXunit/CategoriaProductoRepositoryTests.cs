using ClickMart.Entidades;
using ClickMart.Repositorios;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClickMart.TestXunit
{
    public class CategoriaProductoRepositoryTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"ClickMart_Tests_{Guid.NewGuid()}")
                .Options;

            var ctx = new AppDbContext(opts);

            // Seed categorías
            var cat1 = new CategoriaProducto { CategoriaId = 1, Nombre = "Electrónica" };
            var cat2 = new CategoriaProducto { CategoriaId = 2, Nombre = "Tecnología" };
            ctx.Set<CategoriaProducto>().AddRange(cat1, cat2);

            // Seed un producto asociado a categoría 2 (en uso)
            ctx.Set<Productos>().Add(new Productos
            {
                ProductoId = 10,
                Nombre = "Mouse Pro",
                Precio = 25m,
                Stock = 100,
                CategoriaId = 2 // FK
            });

            ctx.SaveChanges();
            return ctx;
        }

        // HU1007: Creación exitosa
        [Fact]
        public async Task CrearCategoria_Exitoso_retornaEntidadConId()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new CategoriaProductoRepository(ctx);

            var nueva = new CategoriaProducto { Nombre = "Hogar" };
            var guardada = await repo.AddAsync(nueva);

            Assert.NotNull(guardada);
            Assert.True(guardada.CategoriaId > 0);
            var enDb = await ctx.Set<CategoriaProducto>().FindAsync(guardada.CategoriaId);
            Assert.Equal("Hogar", enDb!.Nombre);
        }

        // HU1008: Creación con campos vacíos
        [Fact]
        public async Task CrearCategoria_CamposVacios_lanzaArgumentException()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new CategoriaProductoRepository(ctx);

            var invalida = new CategoriaProducto { Nombre = "   " };
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => repo.AddAsync(invalida));
            Assert.Contains("El nombre de la categoría es obligatorio", ex.Message);
        }

        // HU1009: Visualización de listado
        [Fact]
        public async Task ListarCategorias_retornaListadoCompleto()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new CategoriaProductoRepository(ctx);

            var lista = await repo.GetAllAsync();

            Assert.NotNull(lista);
            Assert.True(lista.Count >= 2);
            Assert.Contains(lista, c => c.Nombre == "Electrónica");
            Assert.Contains(lista, c => c.Nombre == "Tecnología");
        }

        // HU1010: Edición exitosa
        [Fact]
        public async Task EditarCategoria_Exitoso_persisteCambios()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new CategoriaProductoRepository(ctx);

            var cat = await repo.GetByIdAsync(1);
            Assert.NotNull(cat);

            cat!.Nombre = "Electrohogar";
            var ok = await repo.UpdateAsync(cat);
            Assert.True(ok);

            var enDb = await repo.GetByIdAsync(1);
            Assert.Equal("Electrohogar", enDb!.Nombre);
        }

        // HU1011: Edición con campos vacíos
        [Fact]
        public async Task EditarCategoria_CamposVacios_lanzaArgumentException()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new CategoriaProductoRepository(ctx);

            var cat = await repo.GetByIdAsync(1);
            Assert.NotNull(cat);

            cat!.Nombre = "   "; // vacío
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => repo.UpdateAsync(cat));
            Assert.Contains("El nombre de la categoría es obligatorio", ex.Message);
        }

        // HU1012: Eliminación exitosa (sin productos)
        [Fact]
        public async Task EliminarCategoria_SinProductos_devuelveTrue_yLaElimina()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new CategoriaProductoRepository(ctx);

            // Creamos categoría sin productos
            var nueva = await repo.AddAsync(new CategoriaProducto { Nombre = "Decoración" });

            var ok = await repo.DeleteAsync(nueva.CategoriaId);
            Assert.True(ok);

            var enDb = await repo.GetByIdAsync(nueva.CategoriaId);
            Assert.Null(enDb);
        }

        // HU1013: Intento eliminar categoría en uso
        [Fact]
        public async Task EliminarCategoria_EnUso_devuelveFalse_yNoLaElimina()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new CategoriaProductoRepository(ctx);

            // catId = 2 tiene productos asociados
            var ok = await repo.DeleteAsync(2);
            Assert.False(ok);

            var sigue = await repo.GetByIdAsync(2);
            Assert.NotNull(sigue);
            Assert.Equal("Tecnología", sigue!.Nombre);
        }
    }
}