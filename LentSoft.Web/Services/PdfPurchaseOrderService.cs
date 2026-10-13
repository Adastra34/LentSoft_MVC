using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class PdfPurchaseOrderService : IPdfPurchaseOrderService
{
    private static readonly CultureInfo Culture = new("es-CO");

    public PdfPurchaseOrderService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePurchaseOrderPdf(SupplierOrder order)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header().Element(headerContainer => ComposeHeader(headerContainer, order));
                page.Content().Element(contentContainer => ComposeContent(contentContainer, order));
                page.Footer().Element(footerContainer => ComposeFooter(footerContainer));
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, SupplierOrder order)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("LENTSOFT OPTOMETRÍA S.A.S.").FontSize(16).Bold().FontColor("#1E3A8A");
                    column.Item().Text("NIT: 900.123.456-7 | Régimen Común").FontSize(9).Bold().FontColor("#374151");
                    column.Item().Text("Departamento de Compras y Suministros").FontSize(8).FontColor("#6B7280");
                    column.Item().Text("Dirección: Calle Principal # 12-34, Bogotá D.C. - Colombia").FontSize(8).FontColor("#6B7280");
                    column.Item().Text("PBX: +57 (601) 555-0199 | Correo: compras@lentsoft.com").FontSize(8).FontColor("#6B7280");
                });

                row.ConstantItem(240).Column(column =>
                {
                    column.Item().Background("#1E3A8A").Padding(6).Text("ORDEN DE COMPRA")
                        .FontSize(11).Bold().AlignCenter().FontColor(Colors.White);

                    column.Item().Border(1).BorderColor("#1E3A8A").Padding(6).Column(c =>
                    {
                        c.Item().Text($"N° Pedido: {order.NumeroPedido}").FontSize(11).Bold().AlignRight().FontColor("#1E3A8A");
                        c.Item().Text($"Fecha: {order.Fecha.ToLocalTime():dd/MM/yyyy HH:mm}").FontSize(8).AlignRight();

                        var estadoColor = order.Estado.ToLower() switch
                        {
                            "recibido" => "#059669",
                            "parcial" => "#2563EB",
                            "cancelado" => "#DC2626",
                            _ => "#D97706"
                        };

                        c.Item().PaddingTop(2).AlignRight().Text($"ESTADO: {order.Estado.ToUpper()}")
                            .FontSize(9).Bold().FontColor(estadoColor);
                    });
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#CBD5E1");
        });
    }

    private void ComposeContent(IContainer container, SupplierOrder order)
    {
        container.PaddingVertical(10).Column(column =>
        {
            // Proveedor y Envío
            column.Item().Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(8).Column(c =>
                {
                    c.Item().Text("DATOS DEL PROVEEDOR").FontSize(9).Bold().FontColor("#1E3A8A");
                    c.Item().PaddingTop(4).Text($"Razón Social: {order.Supplier?.Nombre ?? "N/A"}").FontSize(8.5f).Bold();
                    c.Item().Text($"NIT / Código: {order.Supplier?.Id ?? order.SupplierId}").FontSize(8);
                    c.Item().Text($"Contacto: {order.Supplier?.Contacto ?? "No registrado"}").FontSize(8);
                    c.Item().Text($"Teléfono: {order.Supplier?.Telefono ?? "No registrado"}").FontSize(8);
                    c.Item().Text($"Email: {order.Supplier?.Email ?? "No registrado"}").FontSize(8);
                    c.Item().Text($"Dirección: {order.Supplier?.Direccion ?? "No registrada"}").FontSize(8);
                });

                row.ConstantItem(15);

                row.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(8).Column(c =>
                {
                    c.Item().Text("CONDICIONES DE ENTREGA").FontSize(9).Bold().FontColor("#1E3A8A");
                    c.Item().PaddingTop(4).Text("Lugar de Entrega: Sede Central LentSoft Bogotá").FontSize(8.5f);
                    c.Item().Text($"Tiempo Estimado: {(order.Supplier?.TiempoEntregaDias != null ? $"{order.Supplier.TiempoEntregaDias} días hábiles" : "Inmediato / Según acuerdo")}").FontSize(8);
                    c.Item().Text("Condición de Pago: Factura contra recepción conforme").FontSize(8);
                    c.Item().Text("Moneda: COP ($)").FontSize(8);
                });
            });

            column.Item().PaddingTop(12);

            // Tabla de Productos
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(70);  // SKU
                    cols.RelativeColumn(3);   // Producto / Descripción
                    cols.RelativeColumn(1);   // Cantidad Solicitada
                    cols.RelativeColumn(1);   // Cantidad Recibida
                    cols.RelativeColumn(1.2f);// Precio Unitario
                    cols.RelativeColumn(1.2f);// Total
                });

                table.Header(header =>
                {
                    header.Cell().Background("#F1F5F9").Padding(5).Text("SKU").Bold().FontSize(8).FontColor("#1E293B");
                    header.Cell().Background("#F1F5F9").Padding(5).Text("DESCRIPCIÓN").Bold().FontSize(8).FontColor("#1E293B");
                    header.Cell().Background("#F1F5F9").Padding(5).AlignCenter().Text("CANT. PEDIDA").Bold().FontSize(8).FontColor("#1E293B");
                    header.Cell().Background("#F1F5F9").Padding(5).AlignCenter().Text("CANT. RECIBIDA").Bold().FontSize(8).FontColor("#1E293B");
                    header.Cell().Background("#F1F5F9").Padding(5).AlignRight().Text("PRECIO UNIT.").Bold().FontSize(8).FontColor("#1E293B");
                    header.Cell().Background("#F1F5F9").Padding(5).AlignRight().Text("TOTAL").Bold().FontSize(8).FontColor("#1E293B");
                });

                var sku = !string.IsNullOrWhiteSpace(order.Product?.Sku) ? order.Product.Sku : $"PRD-{order.ProductId:D4}";
                var nombre = order.Product?.Nombre ?? $"Producto #{order.ProductId}";

                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(sku).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Column(c =>
                {
                    c.Item().Text(nombre).Bold().FontSize(8);
                    if (!string.IsNullOrWhiteSpace(order.Product?.Categoria))
                    {
                        c.Item().Text($"Categoría: {order.Product.Categoria} | Marca: {order.Product.Marca ?? "N/A"}").FontSize(7).FontColor("#64748B");
                    }
                });
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignCenter().Text(order.Cantidad.ToString()).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignCenter().Text(order.CantidadRecibida.ToString()).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignRight().Text(order.PrecioUnitario.ToString("C0", Culture)).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignRight().Text(order.Total.ToString("C0", Culture)).Bold().FontSize(8);
            });

            column.Item().PaddingTop(10);

            // Resumen y Observaciones
            column.Item().Row(row =>
            {
                row.RelativeItem(2).Border(1).BorderColor("#E2E8F0").Padding(8).Column(c =>
                {
                    c.Item().Text("OBSERVACIONES / NOTAS").FontSize(8.5f).Bold().FontColor("#1E3A8A");
                    c.Item().PaddingTop(3).Text(!string.IsNullOrWhiteSpace(order.Notas) ? order.Notas : "Sin observaciones adicionales registradas.").FontSize(8).FontColor("#475569");
                });

                row.ConstantItem(15);

                row.RelativeItem(1.5f).Border(1).BorderColor("#E2E8F0").Padding(8).Column(c =>
                {
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Subtotal:").FontSize(8.5f);
                        r.RelativeItem().AlignRight().Text(order.Total.ToString("C0", Culture)).FontSize(8.5f);
                    });
                    c.Item().PaddingTop(2).Row(r =>
                    {
                        r.RelativeItem().Text("IVA (0%):").FontSize(8.5f);
                        r.RelativeItem().AlignRight().Text("$ 0").FontSize(8.5f);
                    });
                    c.Item().PaddingTop(4).LineHorizontal(1).LineColor("#CBD5E1");
                    c.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text("TOTAL:").Bold().FontSize(10).FontColor("#1E3A8A");
                        r.RelativeItem().AlignRight().Text(order.Total.ToString("C0", Culture)).Bold().FontSize(10).FontColor("#1E3A8A");
                    });
                });
            });

            column.Item().PaddingTop(35);

            // Firmas
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().LineHorizontal(1).LineColor("#64748B");
                    c.Item().PaddingTop(4).AlignCenter().Text("Elaborado y Aprobado por").FontSize(8).Bold();
                    c.Item().AlignCenter().Text("LentSoft - Dpto. de Compras").FontSize(7.5f).FontColor("#64748B");
                });

                row.ConstantItem(50);

                row.RelativeItem().Column(c =>
                {
                    c.Item().LineHorizontal(1).LineColor("#64748B");
                    c.Item().PaddingTop(4).AlignCenter().Text("Recibido a Conformidad Proveedor").FontSize(8).Bold();
                    c.Item().AlignCenter().Text("Firma y Sello del Proveedor").FontSize(7.5f).FontColor("#64748B");
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor("#E2E8F0");
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text("LentSoft Optometría S.A.S. - Sistema de Gestión de Inventarios").FontSize(7.5f).FontColor("#94A3B8");
                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        });
    }
}
