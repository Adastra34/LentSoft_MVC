using System.Security.Cryptography;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class PdfInvoiceService : IPdfInvoiceService
{
    public PdfInvoiceService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoicePdf(Invoice invoice)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header().Element(headerContainer => ComposeHeader(headerContainer, invoice));
                page.Content().Element(contentContainer => ComposeContent(contentContainer, invoice));
                page.Footer().Element(footerContainer => ComposeFooter(footerContainer));
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, Invoice invoice)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("LENTSOFT OPTOMETRÍA S.A.S.").FontSize(18).Bold().FontColor("#4C1D95");
                    column.Item().Text("NIT: 900.123.456-7 | IVA Régimen Común").FontSize(9).Bold().FontColor("#374151");
                    column.Item().Text("Actividad Económica 4774 - Comercio al por menor de artículos ópticos").FontSize(8).FontColor("#6B7280");
                    column.Item().Text("Dirección: Calle Principal # 12-34, Bogotá D.C. - Colombia").FontSize(8).FontColor("#6B7280");
                    column.Item().Text("PBX: +57 (601) 555-0199 | Correo: facturacion@lentsoft.com").FontSize(8).FontColor("#6B7280");
                });

                row.ConstantItem(230).Column(column =>
                {
                    column.Item().Background("#7C3AED").Padding(6).Text("FACTURA ELECTRÓNICA DE VENTA")
                        .FontSize(11).Bold().AlignRight().FontColor(Colors.White);

                    column.Item().Border(1).BorderColor("#7C3AED").Padding(6).Column(c =>
                    {
                        c.Item().Text($"N°: {invoice.NumeroFactura}").FontSize(11).Bold().AlignRight().FontColor("#4C1D95");
                        c.Item().Text($"Fecha Emisión: {invoice.FechaEmision.ToLocalTime():dd/MM/yyyy HH:mm}").FontSize(8).AlignRight();
                        if (invoice.FechaPago.HasValue)
                        {
                            c.Item().Text($"Fecha Pago: {invoice.FechaPago.Value.ToLocalTime():dd/MM/yyyy}").FontSize(8).AlignRight();
                        }
                        c.Item().Text($"Medio de Pago: {invoice.MetodoPago ?? "Efectivo"}").FontSize(8).AlignRight();

                        var estadoColor = invoice.Estado.ToLower() switch
                        {
                            "pagada" => "#10B981",
                            "cancelada" => "#EF4444",
                            _ => "#F59E0B"
                        };
                        c.Item().AlignRight().Text($"ESTADO: {invoice.Estado.ToUpper()}")
                            .FontSize(9).Bold().FontColor(estadoColor);
                    });
                });
            });

            // Banner Resolución DIAN
            col.Item().PaddingTop(8).Background("#F3E8FF").Padding(4).AlignCenter().Text(x =>
            {
                x.Span("Autorización de Facturación DIAN Nº 18764028920000 ").Bold().FontSize(7.5f).FontColor("#4C1D95");
                x.Span("| Rango Autorizado: FAC-2026-0001 a FAC-2026-9999 | Vigencia: 24 Meses").FontSize(7.5f).FontColor("#4C1D95");
            });
        });
    }

    private void ComposeContent(IContainer container, Invoice invoice)
    {
        container.PaddingVertical(10).Column(column =>
        {
            column.Item().PaddingBottom(8).LineHorizontal(1).LineColor("#E9D5FF");

            // Información del Cliente y Pedido
            column.Item().Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor("#E9D5FF").Padding(8).Column(col =>
                {
                    col.Item().Text("DATOS DEL ADQUIRIENTE (CLIENTE)").FontSize(9).Bold().FontColor("#4C1D95");
                    var clientName = invoice.Order?.User != null ? invoice.Order.User.NombreCompleto : "Cliente Genérico";
                    col.Item().Text($"Nombre / Razón Social: {clientName}").Bold();
                    if (invoice.Order?.User != null)
                    {
                        col.Item().Text($"Documento (NIT/CC): {invoice.Order.User.TipoDocumento} {invoice.Order.User.NumeroDocumento}");
                        col.Item().Text($"Email Facturación: {invoice.Order.User.Email}");
                        col.Item().Text($"Teléfono Contacto: {invoice.Order.User.Telefono ?? "N/A"}");
                    }
                });

                row.ConstantItem(15); // espacio

                row.RelativeItem().Border(1).BorderColor("#E9D5FF").Padding(8).Column(col =>
                {
                    col.Item().Text("DETALLES DE LA TRANSACCIÓN").FontSize(9).Bold().FontColor("#4C1D95");
                    col.Item().Text($"N° Orden de Venta: #ORD-{invoice.OrderId:D4}").Bold();
                    col.Item().Text($"Forma de Pago: Contado / Venta Directa");
                    col.Item().Text($"Moneda: COP - Peso Colombiano");
                    if (!string.IsNullOrEmpty(invoice.Order?.DireccionEnvio))
                    {
                        col.Item().Text($"Dirección Entrega: {invoice.Order.DireccionEnvio}");
                    }
                });
            });

            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor("#E9D5FF");

            // Tabla de Items del Pedido
            column.Item().Text("DETALLE DE BIENES Y SERVICIOS").FontSize(9).Bold().FontColor("#4C1D95");
            column.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Producto
                    columns.RelativeColumn(1); // Cantidad
                    columns.RelativeColumn(1.5f); // Precio Unitario
                    columns.RelativeColumn(1.2f); // IVA (19%)
                    columns.RelativeColumn(1.5f); // Subtotal
                });

                table.Header(header =>
                {
                    header.Cell().Background("#7C3AED").Padding(5).Text("Descripción Producto / Servicio").Bold().FontColor(Colors.White);
                    header.Cell().Background("#7C3AED").Padding(5).AlignRight().Text("Cant.").Bold().FontColor(Colors.White);
                    header.Cell().Background("#7C3AED").Padding(5).AlignRight().Text("P. Unitario").Bold().FontColor(Colors.White);
                    header.Cell().Background("#7C3AED").Padding(5).AlignRight().Text("IVA Ítem").Bold().FontColor(Colors.White);
                    header.Cell().Background("#7C3AED").Padding(5).AlignRight().Text("Subtotal").Bold().FontColor(Colors.White);
                });

                if (invoice.Order?.OrderItems != null && invoice.Order.OrderItems.Any())
                {
                    foreach (var item in invoice.Order.OrderItems)
                    {
                        var prodName = item.Product?.Nombre ?? $"Producto #{item.ProductId}";
                        var ivaRate = item.Product != null && item.Product.PorcentajeIva >= 0 ? item.Product.PorcentajeIva : 19.00m;
                        var baseUnit = ivaRate > 0 ? (item.PrecioUnitario / (1m + (ivaRate / 100m))) : item.PrecioUnitario;
                        var itemSubtotalBase = baseUnit * item.Cantidad;
                        var itemIva = item.Subtotal - itemSubtotalBase;

                        table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).Text(prodName);
                        table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignRight().Text(item.Cantidad.ToString());
                        table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignRight().Text($"COP ${baseUnit:N0}");
                        table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignRight().Text($"({ivaRate:0.#}%) COP ${itemIva:N0}");
                        table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignRight().Text($"COP ${item.Subtotal:N0}");
                    }
                }
                else
                {
                    table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).Text("Lentes de contacto / Monturas de Optometría");
                    table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignRight().Text("1");
                    table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignRight().Text($"COP ${invoice.Subtotal:N0}");
                    table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignRight().Text($"COP ${invoice.Impuestos:N0}");
                    table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignRight().Text($"COP ${invoice.Total:N0}");
                }
            });

            // Resumen de Totales
            column.Item().PaddingTop(10).AlignRight().Column(totCol =>
            {
                totCol.Item().Row(r =>
                {
                    r.ConstantItem(150).Text("Subtotal Gravado:").AlignRight();
                    r.ConstantItem(110).Text($"COP ${invoice.Subtotal:N0}").Bold().AlignRight();
                });
                totCol.Item().Row(r =>
                {
                    r.ConstantItem(150).Text("Total IVA (Discrim.):").AlignRight();
                    r.ConstantItem(110).Text($"COP ${invoice.Impuestos:N0}").Bold().AlignRight();
                });
                totCol.Item().Row(r =>
                {
                    r.ConstantItem(150).Text("TOTAL FACTURA:").FontSize(11).Bold().FontColor("#4C1D95").AlignRight();
                    r.ConstantItem(110).Text($"COP ${invoice.Total:N0}").FontSize(11).Bold().FontColor("#4C1D95").AlignRight();
                });
            });

            // Bloque Representación Gráfica DIAN CUFE
            var cufeHash = GenerateCufeHash(invoice);
            column.Item().PaddingTop(15).Border(1).BorderColor("#FDE047").Background("#FEFCE8").Padding(8).Column(cuf =>
            {
                cuf.Item().Text("REPRESENTACIÓN GRÁFICA DE LA FACTURA ELECTRÓNICA DE VENTA (DIAN)").FontSize(8).Bold().FontColor("#854D0E");
                cuf.Item().Text($"CUFE (Código Único de Factura Electrónica):").FontSize(7.5f).Bold().FontColor("#374151");
                cuf.Item().Text(cufeHash).FontSize(6.5f).FontFamily("Courier").FontColor("#6B7280");
                cuf.Item().PaddingTop(3).Text("Documento emitido electrónicamente según la Resolución DIAN 000042 de 2020 y Art. 617 del Estatuto Tributario.").FontSize(7).FontColor("#854D0E");
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor("#E9D5FF");
            col.Item().PaddingTop(4).Text(x =>
            {
                x.Span("Factura impresa por software autorizado ").FontSize(7.5f).FontColor("#6B7280");
                x.Span("LentSoft Optometría S.A.S.").Bold().FontSize(7.5f).FontColor("#7C3AED");
                x.Span(" — www.lentsoft.com. Creado en conformidad con la DIAN.").FontSize(7.5f).FontColor("#6B7280");
            });
        });
    }

    private string GenerateCufeHash(Invoice invoice)
    {
        var rawData = $"{invoice.NumeroFactura}{invoice.FechaEmision:yyyyMMddHHmmss}{invoice.Total:F2}{invoice.Impuestos:F2}9001234567";
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        var sb = new StringBuilder();
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
