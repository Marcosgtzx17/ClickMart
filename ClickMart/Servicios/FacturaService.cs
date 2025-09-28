using System.Globalization;
using System.Linq;
using System.IO;
using ClickMart.DTOs.FacturaDTOs;
using ClickMart.Entidades;
using ClickMart.Interfaces;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment _env;

        public FacturaService(
            IPedidoRepository pedidos,
            IDetallePedidoRepository detalles,
            IProductoRepository productos,
            IUsuarioRepository usuarios,
            IWebHostEnvironment env)
        {
            _pedidos = pedidos;
            _detalles = detalles;
            _productos = productos;
            _usuarios = usuarios;
            _env = env;
        }

        public async Task<byte[]?> GenerarFacturaPdfAsync(int pedidoId)
        {
            var pedido = await _pedidos.GetByIdAsync(pedidoId);
            if (pedido is null) return null;

            var usuario = await _usuarios.GetByIdAsync(pedido.UsuarioId);
            var nombreCliente = usuario?.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(nombreCliente))
                nombreCliente = pedido.Usuario?.Nombre ?? $"Usuario {pedido.UsuarioId}";

            var detalles = await _detalles.GetByPedidoAsync(pedido.PedidoId);
            if (detalles is null || detalles.Count == 0) return null;

            // Precarga productos + imágenes
            var prodIds = detalles.Select(d => d.IdProducto).Distinct().ToList();
            var dictProductos = new Dictionary<int, Productos>();
            var dictImagenes = new Dictionary<int, byte[]?>();

            foreach (var id in prodIds)
            {
                var p = await _productos.GetByIdAsync(id);
                if (p != null) dictProductos[id] = p;

                dictImagenes[id] = await _productos.GetImagenAsync(id);
            }

            // ===== FIX: Subtotal robusto (BD puede venir 0/null) =====
            var items = detalles.Select(d =>
            {
                dictProductos.TryGetValue(d.IdProducto, out var prod);

                // Precio unitario: preferimos el del producto; si no hay, derivamos del subtotal de BD
                var precioUnitario = prod != null
                    ? ToDecimal(prod.Precio)
                    : (d.Cantidad > 0 ? ToDecimal(d.Subtotal) / d.Cantidad : 0m);

                // Subtotal: si en BD viene 0/null, calculamos Cantidad * PrecioUnitario
                var subtotalDb = ToDecimal(d.Subtotal);
                var subtotalCalc = precioUnitario * d.Cantidad;
                var subtotal = subtotalDb > 0m ? subtotalDb : subtotalCalc;

                // Redondeo contable
                precioUnitario = RoundMoney(precioUnitario);
                subtotal = RoundMoney(subtotal);

                return new FacturaItemDTO
                {
                    Producto = prod != null ? prod.Nombre : $"Producto {d.IdProducto}",
                    Cantidad = d.Cantidad,
                    PrecioUnitario = precioUnitario,
                    Subtotal = subtotal,
                    ImagenProducto = dictImagenes.TryGetValue(d.IdProducto, out var img) ? img : null
                };
            }).ToList();

            var factura = new FacturaDTO
            {
                PedidoId = pedido.PedidoId,
                FechaEmision = DateTime.Now, // o pedido.Fecha
                Usuario = nombreCliente,
                Total = RoundMoney(items.Sum(i => i.Subtotal)),
                Items = items
            };

            return GenerarPdf(factura);
        }

        // === Helpers ===
        private static decimal ToDecimal(decimal? value) => value ?? 0m;
        private static decimal RoundMoney(decimal value) =>
            decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        private byte[]? LoadLogo()
        {
            try
            {
                var basePath = _env.WebRootPath ?? _env.ContentRootPath;
                var path = Path.Combine(basePath, "img", "clickmart-logo.png");
                return File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            catch { return null; }
        }

        private byte[] GenerarPdf(FacturaDTO m)
        {
            var culture = new CultureInfo("es-ES");
            var logo = LoadLogo();
            culture.NumberFormat.CurrencySymbol = "$";
            culture.NumberFormat.CurrencyPositivePattern = 0; // $n
            culture.NumberFormat.CurrencyNegativePattern = 1; // -$n

            byte[] pdf = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);

                    // ===== HEADER =====
                    page.Header().Row(row =>
                    {
                        row.ConstantItem(150).Height(60).AlignLeft().Column(col =>
                        {
                            if (logo is not null && logo.Length > 0)
                                col.Item().Image(logo).FitArea();
                            else
                                col.Item().Text("ClickMart").Bold().FontSize(22);
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text($"Factura N° {m.PedidoId}").SemiBold().FontSize(12);
                            col.Item().Text($"Fecha de emisión: {m.FechaEmision:yyyy-MM-dd HH:mm}");
                        });
                    });

                    // ===== CONTENT =====
                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text($"Cliente: {m.Usuario}").FontSize(12);
                        col.Item().Border(0.5f).Background(Colors.Purple.Lighten4).Height(2);

                        // Tabla de items
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(58); // Img
                                c.RelativeColumn(4);  // Producto
                                c.RelativeColumn(1);  // Cantidad
                                c.RelativeColumn(2);  // Precio
                                c.RelativeColumn(2);  // Subtotal
                            });

                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text("Img");
                                h.Cell().Element(HeaderCell).Text("Producto");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Cantidad");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Precio");
                                h.Cell().Element(HeaderCell).AlignRight().Text("Subtotal");
                            });

                            foreach (var it in m.Items)
                            {
                                table.Cell().Element(c =>
                                    BodyCell(c).MinHeight(48).MinWidth(48).Padding(2)
                                               .Border(0.5f).AlignCenter().AlignMiddle()
                                ).Element(e =>
                                {
                                    if (it.ImagenProducto is not null && it.ImagenProducto.Length > 0)
                                        e.Image(it.ImagenProducto).FitArea();
                                    else
                                        e.Text("—").Light().FontSize(8);
                                });

                                table.Cell().Element(BodyCell).Text(it.Producto);
                                table.Cell().Element(BodyCell).AlignRight().Text(it.Cantidad.ToString());
                                table.Cell().Element(BodyCell).AlignRight().Text(it.PrecioUnitario.ToString("C", culture));
                                table.Cell().Element(BodyCell).AlignRight().Text(it.Subtotal.ToString("C", culture));
                            }
                        });

                        col.Item().AlignRight().Text($"Total: {m.Total.ToString("C", culture)}")
                            .Bold().FontSize(14);
                    });

                    // ===== FOOTER =====
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
