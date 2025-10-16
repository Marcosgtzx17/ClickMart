using ClickMart.Entidades;
using ClickMart.Repositorios;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClickMart.TestXunit
{
    public class DistribuidorRepositoryTests
    {
        // ===== Helper: DbContext InMemory con semilla =====
        private AppDbContext GetInMemoryDbContext()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"ClickMart_Dist_Tests_{Guid.NewGuid()}")
                .Options;

            var ctx = new AppDbContext(opts);

            // Seed: dos distribuidores
            var d1 = new Distribuidor { DistribuidorId = 1, Nombre = "Acme", Telefono = "111-111", Direccion = "Calle 1", Gmail = "ventas@acme.com" };
            var d2 = new Distribuidor { DistribuidorId = 2, Nombre = "Globex", Telefono = "222-222", Direccion = "Av. 2", Gmail = "ventas@globex.com" };
            ctx.Set<Distribuidor>().AddRange(d1, d2);

            // Seed: un producto que referencia a Globex (para HU-1021)
            ctx.Set<Productos>().Add(new Productos
            {
                ProductoId = 10,
                Nombre = "Mouse Pro",
                Precio = 20m,
                Stock = 50,
                CategoriaId = 1,
                DistribuidorId = 2 // FK -> Globex
            });

            ctx.SaveChanges();
            return ctx;
        }

        // ===== HU 7 (1015): Creación exitosa =====
        [Fact]
        public async Task CrearDistribuidor_Exitoso_retornaEntidadConId()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new DistribuidorRepository(ctx);

            var nuevo = new Distribuidor
            {
                Nombre = "  Umbrella  ",
                Telefono = " 333-333 ",
                Direccion = "  Calle Umbrella ",
                Gmail = "  contacto@umbrella.com "
            };

            var saved = await repo.AddAsync(nuevo);

            Assert.NotNull(saved);
            Assert.True(saved.DistribuidorId > 0);

            var enDb = await repo.GetByIdAsync(saved.DistribuidorId);
            Assert.Equal("Umbrella", enDb!.Nombre);          // Trim aplicado
            Assert.Equal("333-333", enDb.Telefono);
            Assert.Equal("Calle Umbrella", enDb.Direccion);
            Assert.Equal("contacto@umbrella.com", enDb.Gmail);
        }

        // ===== HU 7 (1016): Validación de campos obligatorios =====
        [Fact]
        public async Task CrearDistribuidor_CamposVacios_lanzaArgumentException()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new DistribuidorRepository(ctx);

            var invalido = new Distribuidor
            {
                Nombre = "   ",
                Telefono = "",
                Direccion = "   ",
                Gmail = ""
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => repo.AddAsync(invalido));
            Assert.Contains("Complete todos los campos obligatorios", ex.Message);
        }

        // ===== HU 8 (1017): Visualización del listado =====
        [Fact]
        public async Task ListarDistribuidores_retornaOrdenadoPorNombre()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new DistribuidorRepository(ctx);

            var lista = await repo.GetAllAsync();

            Assert.True(lista.Count >= 2);
            // Verificar orden por Nombre ascendente
            var nombres = lista.Select(x => x.Nombre).ToList();
            var ordenados = nombres.OrderBy(x => x).ToList();
            Assert.Equal(ordenados, nombres);
            Assert.Contains(lista, d => d.Nombre == "Acme");
            Assert.Contains(lista, d => d.Nombre == "Globex");
        }

        // ===== HU 9 (1018): Edición exitosa =====
        [Fact]
        public async Task EditarDistribuidor_Exitoso_persisteCambios()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new DistribuidorRepository(ctx);

            var dist = await repo.GetByIdAsync(1);
            Assert.NotNull(dist);

            dist!.Nombre = "Acme Corp";
            dist.Telefono = "111-999";
            dist.Direccion = "Boulevard 1";
            dist.Gmail = "ventas@acmecorp.com";

            var ok = await repo.UpdateAsync(dist);
            Assert.True(ok);

            var enDb = await repo.GetByIdAsync(1);
            Assert.Equal("Acme Corp", enDb!.Nombre);
            Assert.Equal("111-999", enDb.Telefono);
            Assert.Equal("Boulevard 1", enDb.Direccion);
            Assert.Equal("ventas@acmecorp.com", enDb.Gmail);
        }

        // ===== HU 9 (1019): Validación al editar =====
        [Fact]
        public async Task EditarDistribuidor_CamposVacios_lanzaArgumentException()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new DistribuidorRepository(ctx);

            var dist = await repo.GetByIdAsync(1);
            Assert.NotNull(dist);

            dist!.Nombre = "   "; // inválido
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => repo.UpdateAsync(dist));
            Assert.Contains("Complete todos los campos obligatorios", ex.Message);
        }

        // ===== HU 10 (1020): Eliminación exitosa (sin productos) =====
        [Fact]
        public async Task EliminarDistribuidor_SinProductos_devuelveTrue_yLoElimina()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new DistribuidorRepository(ctx);

            // Creamos un distribuidor sin productos asociados
            var nuevo = await repo.AddAsync(new Distribuidor
            {
                Nombre = "DecorMax",
                Telefono = "444-444",
                Direccion = "Calle 4",
                Gmail = "contacto@decormax.com"
            });

            var ok = await repo.DeleteAsync(nuevo.DistribuidorId);
            Assert.True(ok);

            var enDb = await repo.GetByIdAsync(nuevo.DistribuidorId);
            Assert.Null(enDb);
        }

        // ===== HU 10 (1021): Restricción por referencias activas =====
        [Fact]
        public async Task EliminarDistribuidor_EnUso_devuelveFalse_yNoElimina()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new DistribuidorRepository(ctx);

            // DistribuidorId = 2 está referenciado por un Producto en la semilla
            var ok = await repo.DeleteAsync(2);
            Assert.False(ok);

            var sigue = await repo.GetByIdAsync(2);
            Assert.NotNull(sigue);
            Assert.Equal("Globex", sigue!.Nombre);
        }
    }
}