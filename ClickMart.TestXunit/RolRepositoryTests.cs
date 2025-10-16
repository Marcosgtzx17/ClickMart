using ClickMart.Entidades;
using ClickMart.Repositorios;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClickMart.TestXunit
{
    public class RolRepositoryTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"ClickMart_Roles_{Guid.NewGuid()}")
                .Options;

            var ctx = new AppDbContext(opts);

            // Seed: dos roles y un usuario asociado al rol "Usuario" (RolId=2)
            var r1 = new Rol { RolId = 1, Nombre = "Administrador" };
            var r2 = new Rol { RolId = 2, Nombre = "Cliente" };
            ctx.Roles.AddRange(r1, r2);

            ctx.Set<Usuario>().Add(new Usuario
            {
                UsuarioId = 101,
                Nombre = "Henry",
                Email = "henry@clickmart.com",
                PasswordHash = "hash",
                RolId = 2
            });

            ctx.SaveChanges();
            return ctx;
        }

        // HU-1022: Creación exitosa de rol
        [Fact]
        public async Task CrearRol_Exitoso_retornaEntidadConId()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new RolRepository(ctx);

            var nuevo = new Rol { Nombre = "  Editor  " };
            var saved = await repo.AddAsync(nuevo);

            Assert.NotNull(saved);
            Assert.True(saved.RolId > 0);

            var enDb = await repo.GetByIdAsync(saved.RolId);
            Assert.Equal("Editor", enDb!.Nombre); // Trim aplicado
        }

        // HU-1023: Validación de campos obligatorios
        [Fact]
        public async Task CrearRol_CampoNombreVacio_lanzaArgumentException()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new RolRepository(ctx);

            var invalido = new Rol { Nombre = "   " };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => repo.AddAsync(invalido));
            Assert.Contains("El nombre del rol es obligatorio", ex.Message);
        }

        // HU-1024: Visualización del listado de roles
        [Fact]
        public async Task ListarRoles_retornaListadoCompleto()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new RolRepository(ctx);

            var lista = await repo.GetAllAsync();

            Assert.NotNull(lista);
            Assert.True(lista.Count >= 2);
            Assert.Contains(lista, r => r.Nombre == "Administrador");
            Assert.Contains(lista, r => r.Nombre == "Cliente");
        }

        // HU-1025: Edición exitosa de rol
        [Fact]
        public async Task EditarRol_Exitoso_persisteCambios()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new RolRepository(ctx);

            var rol = await repo.GetByIdAsync(1);
            Assert.NotNull(rol);

            rol!.Nombre = "SuperAdmin";
            var ok = await repo.UpdateAsync(rol);
            Assert.True(ok);

            var enDb = await repo.GetByIdAsync(1);
            Assert.Equal("SuperAdmin", enDb!.Nombre);
        }

        // HU-1026: Validación al editar roles (nombre vacío)
        [Fact]
        public async Task EditarRol_NombreVacio_lanzaArgumentException()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new RolRepository(ctx);

            var rol = await repo.GetByIdAsync(1);
            Assert.NotNull(rol);

            rol!.Nombre = "   ";
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => repo.UpdateAsync(rol));
            Assert.Contains("El nombre del rol es obligatorio", ex.Message);
        }

        // HU-1027: Eliminación exitosa (sin usuarios asociados)
        [Fact]
        public async Task EliminarRol_SinUsuarios_devuelveTrue_yLoElimina()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new RolRepository(ctx);

            // Crear un rol temporal sin usuarios
            var temporal = await repo.AddAsync(new Rol { Nombre = "Temporal" });

            var ok = await repo.DeleteAsync(temporal.RolId);
            Assert.True(ok);

            var enDb = await repo.GetByIdAsync(temporal.RolId);
            Assert.Null(enDb);
        }

        // HU-1028: Restricción por referencias activas (usuarios asociados)
        [Fact]
        public async Task EliminarRol_ConUsuariosAsociados_devuelveFalse_yNoElimina()
        {
            var ctx = GetInMemoryDbContext();
            var repo = new RolRepository(ctx);

            // RolId=2 ("Usuario") está en uso por el seed
            var ok = await repo.DeleteAsync(2);
            Assert.False(ok);

            var sigue = await repo.GetByIdAsync(2);
            Assert.NotNull(sigue);
            Assert.Equal("Cliente", sigue!.Nombre);
        }
    }
}