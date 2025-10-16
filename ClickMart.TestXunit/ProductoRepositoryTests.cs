using ClickMart.Entidades;
using ClickMart.Repositorios;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClickMart.TestXunit
{
    public class ProductoRepositoryTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"ClickMart_Prod_{Guid.NewGuid()}")
                .Options;

            var ctx = new AppDbContext(opts);

            // Seed mínimos: categoría, distribuidor, productos base, reseña ligada a producto 2
            ctx.Set<CategoriaProducto>().AddRange(
                new CategoriaProducto { CategoriaId = 1, Nombre = "Electrónica" },
                new CategoriaProducto { CategoriaId = 2, Nombre = "Ropa" }
            );

            ctx.Set<Distribuidor>().AddRange(
                new Distribuidor { DistribuidorId = 1, Nombre = "Acme", Telefono = "111", Direccion = "C1", Gmail = "a@a.com" },
                new Distribuidor { DistribuidorId = 2, Nombre = "Globex", Telefono = "222", Direccion = "C2", Gmail = "g@g.com" }
            );

            var p1 = new Productos
            {
                ProductoId = 1,
                Nombre = "Mouse",
                Precio = 20m,
                Stock = 50,
                CategoriaId = 1,
                DistribuidorId = 1,
                Marca = "Logi",
                Talla = "M",
                Imagen = null
            };

            var p2 = new Productos
            {
                ProductoId = 2,
                Nombre = "Teclado",
                Precio = 30m,
                Stock = 30,
                CategoriaId = 1,
                DistribuidorId = 2,
                Marca = "MK",
                Talla = "L",
                Imagen = null
            };

            ctx.Set<Productos>().AddRange(p1, p2);

            // Dependencia: reseña apunta a producto 2 (bloqueo de borrado)
            ctx.Set<Resena>().Add(new Resena
            {
                ResenaId = 200,
                UsuarioId = 10,
                ProductoId = 2,
                Calificacion = 5,
                Comentario = "Top!"
            });

            ctx.SaveChanges();
            return ctx;
        }

        // ===== HU-019 (1036): Creación exitosa de producto =====
        [Fact]
        public async Task CrearProducto_Exitoso_retornaEntidadConId()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new ProductoRepository(ctx);

            var nuevo = new Productos
            {
                Nombre = "  Headset  ",
                Precio = 99.9m,
                Stock = 15,
                CategoriaId = 1,
                DistribuidorId = 1,
                Marca = "  Hyper ",
                Talla = "  U "
            };

            var saved = await repo.AddAsync(nuevo);

            Assert.NotNull(saved);
            Assert.True(saved.ProductoId > 0);

            var enDb = await repo.GetByIdAsync(saved.ProductoId);
            Assert.Equal("Headset", enDb!.Nombre);   // Trim aplicado
            Assert.Equal(99.9m, enDb.Precio);
            Assert.Equal(15, enDb.Stock);
            Assert.Equal(1, enDb.CategoriaId);
            Assert.Equal(1, enDb.DistribuidorId);
        }

        // ===== HU-019 (1037): Validación de campos obligatorios =====
        [Fact]
        public async Task CrearProducto_CamposInvalidos_lanzaArgumentException()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new ProductoRepository(ctx);

            var invalido = new Productos
            {
                Nombre = "   ",     // vacío
                Precio = 0m,        // inválido
                Stock = -1,         // inválido
                CategoriaId = 0,    // inválido
                DistribuidorId = 0  // inválido
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => repo.AddAsync(invalido));
            Assert.Contains("Complete todos los campos obligatorios", ex.Message);
        }

        // ===== HU-020 (1038): Visualización del listado de productos =====
        [Fact]
        public async Task ListarProductos_retornaListadoBasico()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new ProductoRepository(ctx);

            var lista = await repo.GetAllAsync();

            Assert.NotNull(lista);
            Assert.True(lista.Count >= 2);
            Assert.Contains(lista, p => p.Nombre == "Mouse" && p.Precio == 20m);
            Assert.Contains(lista, p => p.CategoriaId == 1);
        }

        // ===== HU-021 (1039): Edición exitosa de producto =====
        [Fact]
        public async Task EditarProducto_Exitoso_persisteCambios()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new ProductoRepository(ctx);

            var prod = await repo.GetByIdAsync(1);
            Assert.NotNull(prod);

            prod!.Nombre = "Mouse Pro";
            prod.Precio = 25m;
            prod.Stock = 40;

            var ok = await repo.UpdateAsync(prod);
            Assert.True(ok);

            var enDb = await repo.GetByIdAsync(1);
            Assert.Equal("Mouse Pro", enDb!.Nombre);
            Assert.Equal(25m, enDb.Precio);
            Assert.Equal(40, enDb.Stock);
        }

        // ===== HU-021 (1040): Validación al editar =====
        [Fact]
        public async Task EditarProducto_DatosInvalidos_lanzaArgumentException()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new ProductoRepository(ctx);

            var prod = await repo.GetByIdAsync(1);
            Assert.NotNull(prod);

            prod!.Precio = 0m; // inválido
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => repo.UpdateAsync(prod));
            Assert.Contains("Complete todos los campos obligatorios", ex.Message);
        }

        // ===== HU-022 (1041): Eliminación exitosa sin dependencias =====
        [Fact]
        public async Task EliminarProducto_SinDependencias_devuelveTrue_yLoElimina()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new ProductoRepository(ctx);

            // Creamos uno sin dependencias
            var nuevo = await repo.AddAsync(new Productos
            {
                Nombre = "Pad Mouse",
                Precio = 10m,
                Stock = 5,
                CategoriaId = 1,
                DistribuidorId = 1
            });

            var ok = await repo.DeleteAsync(nuevo.ProductoId);
            Assert.True(ok);

            var enDb = await repo.GetByIdAsync(nuevo.ProductoId);
            Assert.Null(enDb);
        }

        // ===== HU-022 (1042): Restricción por dependencias activas =====
        [Fact]
        public async Task EliminarProducto_ConResenas_devuelveFalse_yNoElimina()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new ProductoRepository(ctx);

            // ProductoId = 2 tiene reseña semillada
            var ok = await repo.DeleteAsync(2);
            Assert.False(ok);

            var sigue = await repo.GetByIdAsync(2);
            Assert.NotNull(sigue);
            Assert.Equal("Teclado", sigue!.Nombre);
        }

        // ===== HU-023 (1043): Visualización de imagen pública =====
        [Fact]
        public async Task ImagenPublica_GetImagen_devuelveBytes()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new ProductoRepository(ctx);

            var bytes = new byte[] { 1, 2, 3, 4 };
            var upd = await repo.UpdateImagenAsync(1, bytes);
            Assert.True(upd);

            var img = await repo.GetImagenAsync(1);
            Assert.NotNull(img);
            Assert.True(bytes.SequenceEqual(img!));
        }

        // ===== HU-024 (1044): Imagen no disponible =====
        [Fact]
        public async Task ImagenNoDisponible_GetImagen_devuelveNull()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new ProductoRepository(ctx);

            // Producto 2 quedó sin imagen en el seed
            var img = await repo.GetImagenAsync(2);
            Assert.Null(img);
        }
    }
}