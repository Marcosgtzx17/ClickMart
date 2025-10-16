using ClickMart.DTOs.UsuariosDTOs;
using ClickMart.Entidades;
using ClickMart.Repositorios;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClickMart.TestXunit
{
    public class UsuarioRepositoryTests
    {
        // ===== Helper: DbContext InMemory con semilla =====
        private AppDbContext GetInMemoryDbContext()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"ClickMart_Usuarios_{Guid.NewGuid()}")
                .Options;

            var ctx = new AppDbContext(opts);

            // Roles base
            ctx.Roles.AddRange(
                new Rol { RolId = 1, Nombre = "Administrador" },
                new Rol { RolId = 2, Nombre = "Cliente" }
            );

            // Usuarios base
            var u1 = new Usuario
            {
                UsuarioId = 1,
                Nombre = "Henry",
                Email = "henry@clickmart.com",
                Telefono = "7777-7777",
                Direccion = "Calle 1",
                PasswordHash = "hash",
                RolId = 2
            };
            var u2 = new Usuario
            {
                UsuarioId = 2,
                Nombre = "Alice",
                Email = "alice@clickmart.com",
                Telefono = "8888-8888",
                Direccion = "Av 2",
                PasswordHash = "hash",
                RolId = 1
            };

            ctx.Usuarios.AddRange(u1, u2);

            // Dependencias para restricción de borrado (HU-1034)
            ctx.Set<Pedido>().Add(new Pedido
            {
                PedidoId = 100,
                UsuarioId = 2, // Alice en uso
                Fecha = DateTime.UtcNow,
                PagoEstado = EstadoPago.PENDIENTE,
                Total = 50m
            });

            ctx.Set<Resena>().Add(new Resena
            {
                ResenaId = 200,
                UsuarioId = 2, // Alice también tiene reseña
                ProductoId = 10,
                Calificacion = 5,
                Comentario = "Top"
            });

            ctx.SaveChanges();
            return ctx;
        }

        // ===== HU-015 (1029): Creación exitosa de usuario =====
        [Fact]
        public async Task HU1029_CrearUsuario_Exitoso_retornaEntidadConId()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new UsuarioRepository(ctx);

            var nuevo = new Usuario
            {
                Nombre = "  Bob  ",
                Email = " bob@clickmart.com ",
                Telefono = " 9999-9999 ",
                Direccion = "   Calle Bob  ",
                PasswordHash = "hash",
                RolId = 2
            };

            var saved = await repo.AddAsync(nuevo);

            Assert.NotNull(saved);
            Assert.True(saved.UsuarioId > 0);

            var enDb = await repo.GetByIdAsync(saved.UsuarioId);
            Assert.Equal("Bob", enDb!.Nombre);
            Assert.Equal("bob@clickmart.com", enDb.Email);
            Assert.Equal("9999-9999", enDb.Telefono);
            Assert.Equal("Calle Bob", enDb.Direccion);
        }

        // ===== HU-016 (1030): Validación de campos obligatorios =====
        [Fact]
        public async Task HU1030_CrearUsuario_CamposVacios_lanzaArgumentException()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new UsuarioRepository(ctx);

            var invalido = new Usuario
            {
                Nombre = "   ",
                Email = "   ",
                Telefono = "",
                Direccion = "",
                PasswordHash = "hash",
                RolId = 2
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => repo.AddAsync(invalido));
            Assert.Contains("Complete todos los campos obligatorios", ex.Message);
        }

        [Fact]
        public async Task HU1030_CrearUsuario_EmailInvalido_lanzaArgumentException()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new UsuarioRepository(ctx);

            var invalido = new Usuario
            {
                Nombre = "Test",
                Email = "correo-no-valido",
                Telefono = "1111-1111",
                Direccion = "Calle X",
                PasswordHash = "hash",
                RolId = 2
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => repo.AddAsync(invalido));
            Assert.Contains("Formato de correo inválido", ex.Message);
        }

        // ===== HU-017 (1031): Visualización del listado =====
        [Fact]
        public async Task HU1031_ListarUsuarios_retornaListadoConCamposClave()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new UsuarioRepository(ctx);

            var lista = await repo.GetAllUsuariosAsync();

            Assert.NotNull(lista);
            Assert.True(lista.Count >= 2);
            var henry = lista.FirstOrDefault(x => x.Email == "henry@clickmart.com");
            Assert.NotNull(henry);
            Assert.Equal("Henry", henry!.Nombre);
            Assert.Equal("7777-7777", henry.Telefono);
            Assert.Equal("Calle 1", henry.Direccion);
            Assert.Equal("Cliente", henry.Rol); // por include de Rol
        }

        // ===== HU-017 (1032): Edición exitosa =====
        [Fact]
        public async Task HU1032_EditarUsuario_Exitoso_persisteCambios()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new UsuarioRepository(ctx);

            var user = await repo.GetByIdAsync(1);
            Assert.NotNull(user);

            user!.Nombre = "Henry Updated";
            user.Telefono = "7777-0000";
            user.Direccion = "Boulevard 1";
            user.Email = "henry.up@clickmart.com";

            var ok = await repo.UpdateAsync(user);
            Assert.True(ok);

            var enDb = await repo.GetByIdAsync(1);
            Assert.Equal("Henry Updated", enDb!.Nombre);
            Assert.Equal("7777-0000", enDb.Telefono);
            Assert.Equal("Boulevard 1", enDb.Direccion);
            Assert.Equal("henry.up@clickmart.com", enDb.Email);
        }

        // ===== HU-017 (1033): Validación al editar =====
        [Fact]
        public async Task HU1033_EditarUsuario_DatosInvalidos_lanzaArgumentException()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new UsuarioRepository(ctx);

            var user = await repo.GetByIdAsync(1);
            Assert.NotNull(user);

            user!.Email = "no-es-email";
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => repo.UpdateAsync(user));
            Assert.Contains("Formato de correo inválido", ex.Message);
        }

        // ===== HU-018 (1034): Eliminación exitosa (sin dependencias) =====
        [Fact]
        public async Task HU1034_EliminarUsuario_SinDependencias_devuelveTrue_yLoElimina()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new UsuarioRepository(ctx);

            var nuevo = await repo.AddAsync(new Usuario
            {
                Nombre = "Temp",
                Email = "temp@clickmart.com",
                Telefono = "0000-0000",
                Direccion = "Calle Temp",
                PasswordHash = "hash",
                RolId = 2
            });

            var ok = await repo.DeleteAsync(nuevo.UsuarioId);
            Assert.True(ok);

            var enDb = await repo.GetByIdAsync(nuevo.UsuarioId);
            Assert.Null(enDb);
        }

        // ===== HU-018 (1034): Restricción por dependencias críticas =====
        [Fact]
        public async Task HU1034_EliminarUsuario_ConDependencias_devuelveFalse_yNoElimina()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new UsuarioRepository(ctx);

            // UsuarioId = 2 (Alice) tiene Pedido y Resena en el seed
            var ok = await repo.DeleteAsync(2);
            Assert.False(ok);

            var sigue = await repo.GetByIdAsync(2);
            Assert.NotNull(sigue);
            Assert.Equal("Alice", sigue!.Nombre);
        }
    }
}