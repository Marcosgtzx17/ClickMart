using ClickMart.Entidades;
using ClickMart.Repositorios;
using ClickMart.Servicios;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClickMart.TestXunit
{
    public class CodigoConfirmacionServiceTests
    {
        // Helper: DbContext InMemory limpio por prueba
        private AppDbContext NewCtx()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"ClickMart_Code_{Guid.NewGuid()}")
                .Options;
            return new AppDbContext(opts);
        }

        // HU-1053: Generación de código
        [Fact]
        public async Task GenerarAsync_creaCodigoDe6Digitos_yPersisteNoUsado()
        {
            var ctx = NewCtx();
            var repo = new CodigoConfirmacionRepository(ctx);
            var svc = new CodigoConfirmacionService(repo);

            var start = DateTime.UtcNow.AddSeconds(-2);
            var resp = await svc.GenerarAsync("cliente@clickmart.com");

            Assert.NotNull(resp);
            Assert.True(resp!.IdCodigo > 0);
            Assert.Equal("cliente@clickmart.com", resp.Email);
            Assert.Equal(6, resp.Codigo!.Length);
            Assert.True(resp.Codigo.All(char.IsDigit));
            Assert.Equal(0, resp.Usado);
            Assert.True(resp.FechaGeneracion >= start);

            var enDb = await ctx.Set<CodigoConfirmacion>().FindAsync(resp.IdCodigo);
            Assert.NotNull(enDb);
            Assert.Equal(resp.Codigo, enDb!.Codigo);
            Assert.Equal(0, enDb.Usado);
        }

        // HU-1054: Código inválido o vencido
        [Fact]
        public async Task ValidarAsync_rechazaCodigoIncorrecto_oVencido()
        {
            var ctx = NewCtx();
            var repo = new CodigoConfirmacionRepository(ctx);
            var svc = new CodigoConfirmacionService(repo);

            var generado = await svc.GenerarAsync("cliente@clickmart.com");
            Assert.NotNull(generado);

            // Caso 1: código incorrecto
            var okWrong = await svc.ValidarAsync("cliente@clickmart.com", "999999");
            Assert.False(okWrong);

            // Caso 2: código vencido (simulamos antigüedad > 10 min)
            var registro = await ctx.Set<CodigoConfirmacion>().FindAsync(generado!.IdCodigo);
            registro!.FechaGeneracion = DateTime.UtcNow.AddMinutes(-30);
            ctx.Update(registro);
            await ctx.SaveChangesAsync();

            var okExpired = await svc.ValidarAsync("cliente@clickmart.com", generado.Codigo!);
            Assert.False(okExpired);
        }

        // HU-1055: Confirmación exitosa (marca usado; segundo intento falla)
        [Fact]
        public async Task ValidarAsync_codigoCorrecto_marcaUsado_yNoPermiteReuso()
        {
            var ctx = NewCtx();
            var repo = new CodigoConfirmacionRepository(ctx);
            var svc = new CodigoConfirmacionService(repo);

            var generado = await svc.GenerarAsync("cliente@clickmart.com");
            Assert.NotNull(generado);

            // 1er uso: OK
            var ok = await svc.ValidarAsync("cliente@clickmart.com", generado!.Codigo!);
            Assert.True(ok);

            var enDb = await ctx.Set<CodigoConfirmacion>().FindAsync(generado.IdCodigo);
            Assert.Equal(1, enDb!.Usado);

            // 2do uso del mismo código: debe fallar (ya está usado)
            var again = await svc.ValidarAsync("cliente@clickmart.com", generado.Codigo!);
            Assert.False(again);
        }
    }
}