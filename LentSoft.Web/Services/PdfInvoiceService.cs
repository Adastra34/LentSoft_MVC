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
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(headerContainer => ComposeHeader(headerContainer, invoice));
                page.Content().Element(contentContainer => ComposeContent(contentContainer, invoice));
                page.Footer().Element(footerContainer => ComposeFooter(footerContainer));
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, Invoice invoice)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("LentSoft Optometría").FontSize(20).Bold().FontColor("#4C1D95");
                column.Item().Text("Soluciones Visuales de Alta Calidad").FontSize(9).FontColor("#6B7280");
                column.Item().Text("NIT: 900.123.456-7 | Régimen Común").FontSize(8).FontColor("#6B7280");
                column.Item().Text("Dirección: Calle Principal # 12-34, Bogotá").FontSize(8).FontColor("#6B7280");
                column.Item().Text("Teléfono: +57 (601) 555-0199").FontSize(8).FontColor("#6B7280");
            });

            row.ConstantItem(200).Column(column =>
            {
                column.Item().Text("FACTURA DE VENTA").FontSize(14).Bold().AlignRight().FontColor("#7C3AED");
                column.Item().Text($"N°: {invoice.NumeroFactura}").FontSize(12).Bold().AlignRight();
                column.Item().Text($"Fecha Emisión: {invoice.FechaEmision:dd/MM/yyyy}").FontSize(9).AlignRight();
                if (invoice.FechaPago.HasValue)
                {
                    column.Item().Text($"Fecha Pago: {invoice.FechaPago.Value:dd/MM/yyyy}").FontSize(9).AlignRight();
                }
                var estadoColor = invoice.Estado.ToLower() switch
                {
                    "pagada" => "#10B981",
                    "cancelada" => "#EF4444",
                    _ => "#F59E0B"
                };
                column.Item().AlignRight().Text($"ESTADO: {invoice.Estado.ToUpper()}")
                    .FontSize(10).Bold().FontColor(estadoColor);
            });
        });
    }

    private void ComposeContent(IContainer container, Invoice invoice)
    {
        container.PaddingVertical(10).Column(column =>
        {
            column.Item().PaddingBottom(15).LineHorizontal(1).LineColor("#E9D5FF");

            // Información del Cliente y Pedido
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("DATOS DEL CLIENTE").FontSize(10).Bold().FontColor("#4C1D95");
                    var clientName = invoice.Order?.User != null ? invoice.Order.User.NombreCompleto : "Cliente Genérico";
                    col.Item().Text($"Nombre: {clientName}");
                    if (invoice.Order?.User != null)
                    {
                        col.Item().Text($"Documento: {invoice.Order.User.TipoDocumento} {invoice.Order.User.NumeroDocumento}");
                        col.Item().Text($"Email: {invoice.Order.User.Email}");
                        col.Item().Text($"Teléfono: {invoice.Order.User.Telefono ?? "N/A"}");
                    }
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("DATOS DEL PEDIDO").FontSize(10).Bold().FontColor("#4C1D95");
                    col.Item().Text($"N° Pedido: #ORD-{invoice.OrderId:D4}");
                    col.Item().Text($"Método de Pago: {invoice.MetodoPago ?? "Efectivo / Tarjeta"}");
                    if (!string.IsNullOrEmpty(invoice.Order?.DireccionEnvio))
                    {
                        col.Item().Text($"Dirección Envío: {invoice.Order.DireccionEnvio}");
                    }
                });
            });

            column.Item().PaddingVertical(15).LineHorizontal(1).LineColor("#E9D5FF");

            // Tabla de Items del Pedido
            column.Item().Text("DETALLE DE PRODUCTOS / SERVICIOS").FontSize(10).Bold().FontColor("#4C1D95");
            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Producto
                    columns.RelativeColumn(1); // Cantidad
                    columns.RelativeColumn(1.5f); // Precio Unitario
                    columns.RelativeColumn(1.5f); // Subtotal
                });

                table.Header(header =>
                {
                    header.Cell().Background("#7C3AED").Padding(5).Text("Producto").Bold().FontColor(Colors.White);
                    header.Cell().Background("#7C3AED").Padding(5).AlignRight().Text("Cant.").Bold().FontColor(Colors.White);
                    header.Cell().Background("#7C3AED").Padding(5).AlignRight().Text("P. Unitario").Bold().FontColor(Colors.White);
                    header.Cell().Background("#7C3AED").Padding(5).AlignRight().Text("Subtotal").Bold().FontColor(Colors.White);
                });

                if (invoice.Order?.OrderItems != null && invoice.Order.OrderItems.Any())
                {
                    foreach (var item in invoice.Order.OrderItems)
                    {
                        var prodName = item.Product?.Nombre ?? $"Producto #{item.ProductId}";
                        table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).Text(prodName);
                        table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignRight().Text(item.Cantidad.ToString());
                        table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignRight().Text($"${item.PrecioUnitario:N2}");
                        table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignRight().Text($"${item.Subtotal:N2}");
                    }
                }
                else
                {
                    table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).Text("Servicio / Concepto Factura");
                    table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignRight().Text("1");
                    table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignRight().Text($"${invoice.Subtotal:N2}");
                    table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignRight().Text($"${invoice.Subtotal:N2}");
                }
            });

            // Resumen de Totales
            column.Item().PaddingTop(15).AlignRight().Column(totCol =>
            {
                totCol.Item().Row(r =>
                {
                    r.ConstantItem(120).Text("Subtotal:").AlignRight();
                    r.ConstantItem(100).Text($"${invoice.Subtotal:N2}").Bold().AlignRight();
                });
                totCol.Item().Row(r =>
                {
                    r.ConstantItem(120).Text("Impuestos (19%):").AlignRight();
                    r.ConstantItem(100).Text($"${invoice.Impuestos:N2}").Bold().AlignRight();
                });
                totCol.Item().Row(r =>
                {
                    r.ConstantItem(120).Text("TOTAL:").FontSize(12).Bold().FontColor("#4C1D95").AlignRight();
                    r.ConstantItem(100).Text($"${invoice.Total:N2}").FontSize(12).Bold().FontColor("#4C1D95").AlignRight();
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor("#E9D5FF");
            col.Item().PaddingTop(5).Text(x =>
            {
                x.Span("Gracias por confiar en ").FontSize(8).FontColor("#6B7280");
                x.Span("LentSoft Optometría").Bold().FontSize(8).FontColor("#7C3AED");
                x.Span(". Para soporte o consultas contactar a soporte@lentsoft.com").FontSize(8).FontColor("#6B7280");
            });
        });
    }
}
