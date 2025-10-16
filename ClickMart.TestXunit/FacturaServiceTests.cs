using ClickMart.DTOs.FacturaDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;
using ClickMart.Servicios;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Infrastructure;  // 👈 Agregado para usar LicenseType

namespace ClickMart.TestXunit
{
    public class FacturaServiceTests
    {
        public FacturaServiceTests()
        {
            // 👇 Esto evita el mensaje de QuestPDF durante los tests
            QuestPDF.Settings.License = LicenseType.Community;
        }

        private FacturaService CreateSut(
            out Mock<IPedidoRepository> mockPedidos,
            out Mock<IDetallePedidoRepository> mockDetalles,
            out Mock<IProductoRepository> mockProductos,
            out Mock<IUsuarioRepository> mockUsuarios)
        {
            mockPedidos = new(MockBehavior.Strict);
            mockDetalles = new(MockBehavior.Strict);
            mockProductos = new(MockBehavior.Strict);
            mockUsuarios = new(MockBehavior.Strict);

            var mockEnv = new Mock<IWebHostEnvironment>();
            mockEnv.SetupGet(e => e.WebRootPath).Returns(Directory.GetCurrentDirectory());

            return new FacturaService(
                mockPedidos.Object,
                mockDetalles.Object,
                mockProductos.Object,
                mockUsuarios.Object,
                mockEnv.Object);
        }

        [Fact]
        public async Task GenerarFacturaPDF_Exitoso()
        {
            // Arrange
            var sut = CreateSut(
                out var mockPedidos,
                out var mockDetalles,
                out var mockProductos,
                out var mockUsuarios);

            var pedido = new Pedido { PedidoId = 1, UsuarioId = 10, Fecha = DateTime.Now };
            var usuario = new Usuario { UsuarioId = 10, Nombre = "Kevin José" };

            var detalles = new List<DetallePedido>
            {
                new DetallePedido { IdProducto = 5, Cantidad = 2, Subtotal = 20 },
                new DetallePedido { IdProducto = 6, Cantidad = 1, Subtotal = 10 }
            };

            var prod5 = new Productos { ProductoId = 5, Nombre = "Mouse", Precio = 10 };
            var prod6 = new Productos { ProductoId = 6, Nombre = "Teclado", Precio = 10 };

            mockPedidos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pedido);
            mockUsuarios.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(usuario);
            mockDetalles.Setup(r => r.GetByPedidoAsync(1)).ReturnsAsync(detalles);
            mockProductos.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(prod5);
            mockProductos.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(prod6);
            mockProductos.Setup(r => r.GetImagenAsync(It.IsAny<int>())).ReturnsAsync((byte[]?)null);

            // Act
            var pdfBytes = await sut.GenerarFacturaPdfAsync(1);

            // Assert
            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes!.Length > 100); // PDF no vacío

            // Verificaciones de mocks
            mockPedidos.Verify(r => r.GetByIdAsync(1), Times.Once);
            mockUsuarios.Verify(r => r.GetByIdAsync(10), Times.Once);
            mockDetalles.Verify(r => r.GetByPedidoAsync(1), Times.Once);
            mockProductos.Verify(r => r.GetByIdAsync(5), Times.Once);
            mockProductos.Verify(r => r.GetByIdAsync(6), Times.Once);
        }
    }
}
