using System.Globalization;
using System.Linq; // Sum, Select
using ClickMart.DTOs.FacturaDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClickMart.Servicios
{
    public class FacturaService : IFacturaService
    {
        private readonly IPedidoRepository _pedidos;
        private readonly IDetallePedidoRepository _detalles;
        private readonly IProductoRepository _productos;
        private readonly IUsuarioRepository _usuarios;

        public FacturaService(
            IPedidoRepository pedidos,
            IDetallePedidoRepository detalles,
            IProductoRepository productos,
            IUsuarioRepository usuarios)
        {
            _pedidos = pedidos;
            _detalles = detalles;
            _productos = productos;
            _usuarios = usuarios;
        }

        public async Task<byte[]?> GenerarFacturaPdfAsync(int pedidoId)
        {
            var pedido = await _pedidos.GetByIdAsync(pedidoId);
            if (pedido is null) return null;

            var usuario = await _usuarios.GetByIdAsync(pedido.UsuarioId);
            var detalles = await _detalles.GetByPedidoAsync(pedidoId);

            // Traemos info de producto para nombre y precio unitario
            var prodIds = detalles.Select(d => d.IdProducto).Distinct().ToList();
            var dictProductos = new Dictionary<int, Productos>();
            foreach (var id in prodIds)
            {
                var p = await _productos.GetByIdAsync(id);
                if (p != null) dictProductos[id] = p;
            }

            var items = detalles.Select(d =>
            {
                var tiene = dictProductos.TryGetValue(d.IdProducto, out var prod);

                decimal precioUnitario = tiene
                    ? ToDecimal(prod!.Precio)                                          // <-- NO ‘??’ directo
                    : (d.Cantidad > 0 ? ToDecimal(d.Subtotal) / d.Cantidad : 0m);

                decimal subtotal = ToDecimal(d.Subtotal);                               // <-- normalizado

                return new FacturaItemDTO
                {
                    Producto = tiene ? prod!.Nombre : $"Producto {d.IdProducto}",
                    Cantidad = d.Cantidad,
                    PrecioUnitario = precioUnitario,
                    Subtotal = subtotal
                };
            }).ToList();

            var factura = new FacturaDTO
            {
                PedidoId = pedido.PedidoId,
                FechaEmision = DateTime.Now, // o pedido.Fecha
                Usuario = usuario?.Nombre ?? $"Usuario {pedido.UsuarioId}",
                Total = items.Sum(i => i.Subtotal),
                Items = items
            };

            return GenerarPdf(factura);
        }

        // Helper para unificar decimal? -> decimal
        private static decimal ToDecimal(decimal? value) => value ?? 0m;

        private static byte[] GenerarPdf(FacturaDTO m)
        {
            var culture = new CultureInfo("es-ES");

            byte[] pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Text("ClickMart")
                            .Bold().FontSize(20);
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text($"Factura N° {m.PedidoId}").SemiBold();
                            col.Item().Text($"Fecha de emisión: {m.FechaEmision:yyyy-MM-dd HH:mm}");
                        });
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(10); // spacing en la columna

                        col.Item().Text($"Cliente: {m.Usuario}").FontSize(12);

                        // línea separadora (sin .Spacing aquí)
                        col.Item().LineHorizontal(0.75f);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(6); // Producto
                                c.RelativeColumn(2); // Cantidad
                                c.RelativeColumn(2); // Precio
                                c.RelativeColumn(2); // Subtotal
                            });

                            // Encabezado
                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text("Producto");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Cantidad");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Precio");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Subtotal");
                            });

                            // Filas
                            foreach (var it in m.Items)
                            {
                                table.Cell().Element(BodyCell).Text(it.Producto);
                                table.Cell().Element(BodyCell).AlignRight().Text(it.Cantidad.ToString());
                                table.Cell().Element(BodyCell).AlignRight().Text(it.PrecioUnitario.ToString("C", culture));
                                table.Cell().Element(BodyCell).AlignRight().Text(it.Subtotal.ToString("C", culture));
                            }
                        });

                        col.Item().AlignRight().Text($"Total: {m.Total.ToString("C", culture)}")
                            .Bold().FontSize(14);
                    });

                    page.Footer().AlignCenter().Text("Gracias por su compra").FontSize(10).Light();
                });
            }).GeneratePdf();

            return pdf;

            // Estilos locales
            static IContainer HeaderCell(IContainer c) =>
                c.DefaultTextStyle(x => x.SemiBold())
                 .PaddingVertical(6).PaddingHorizontal(5)
                 .Background(Colors.Grey.Lighten3)
                 .BorderBottom(1);

            static IContainer BodyCell(IContainer c) =>
                c.PaddingVertical(5).PaddingHorizontal(5)
                 .BorderBottom(0.5f);
        }
    }
}
