using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class PdfReciboService : IPdfReciboService
{
    public PdfReciboService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateReciboPdf(PagoVenta pago)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header().Element(headerContainer => ComposeHeader(headerContainer, pago));
                page.Content().Element(contentContainer => ComposeContent(contentContainer, pago));
                page.Footer().Element(footerContainer => ComposeFooter(footerContainer));
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, PagoVenta pago)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("LENTSOFT OPTOMETRÍA S.A.S.").FontSize(14).Bold().FontColor("#1E3A8A");
                    column.Item().Text("NIT: 900.123.456-7 | Comprobante de Pago").FontSize(8).Bold().FontColor("#374151");
                    column.Item().Text("Dirección: Calle Principal # 12-34, Bogotá D.C.").FontSize(8).FontColor("#6B7280");
                    column.Item().Text("PBX: +57 (601) 555-0199").FontSize(8).FontColor("#6B7280");
                });

                row.ConstantItem(180).Column(column =>
                {
                    column.Item().Background("#2563EB").Padding(5).Text("RECIBO DE ABONO / PAGO")
                        .FontSize(10).Bold().AlignRight().FontColor(Colors.White);

                    column.Item().Border(1).BorderColor("#2563EB").Padding(5).Column(c =>
                    {
                        c.Item().Text($"N°: {pago.NumeroComprobante}").FontSize(10).Bold().AlignRight().FontColor("#1E3A8A");
                        c.Item().Text($"Fecha: {pago.FechaPago.ToLocalTime():dd/MM/yyyy HH:mm}").FontSize(8).AlignRight();
                    });
                });
            });
        });
    }

    private void ComposeContent(IContainer container, PagoVenta pago)
    {
        container.PaddingVertical(10).Column(column =>
        {
            column.Item().PaddingBottom(6).LineHorizontal(1).LineColor("#DBEAFE");

            var venta = pago.Venta;
            var clienteNombre = venta?.User != null ? venta.User.NombreCompleto : "Cliente Genérico";
            var clienteDoc = venta?.User != null ? $"{venta.User.TipoDocumento} {venta.User.NumeroDocumento}" : "N/A";
            var clienteEmail = venta?.User?.Email ?? "N/A";

            var totalVenta = venta?.Total ?? 0m;
            var totalAbonado = venta?.Pagos != null && venta.Pagos.Any() ? venta.Pagos.Sum(p => p.Monto) : pago.Monto;
            var saldoRestante = Math.Max(0m, totalVenta - totalAbonado);

            // Datos Cliente y Venta
            column.Item().Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor("#DBEAFE").Padding(6).Column(col =>
                {
                    col.Item().Text("DATOS DEL CLIENTE").FontSize(8.5f).Bold().FontColor("#1E3A8A");
                    col.Item().Text($"Cliente: {clienteNombre}").Bold();
                    col.Item().Text($"Documento: {clienteDoc}");
                    col.Item().Text($"Correo: {clienteEmail}");
                });

                row.ConstantItem(10);

                row.RelativeItem().Border(1).BorderColor("#DBEAFE").Padding(6).Column(col =>
                {
                    col.Item().Text("DETALLES DE LA VENTA").FontSize(8.5f).Bold().FontColor("#1E3A8A");
                    col.Item().Text($"N° Orden: #ORD-{pago.VentaId:D4}").Bold();
                    col.Item().Text($"Método de Pago: {pago.MetodoPago}");
                    col.Item().Text($"Responsable: {pago.Responsable}");
                });
            });

            column.Item().PaddingVertical(8).LineHorizontal(1).LineColor("#DBEAFE");

            // Resumen Financiero del Abono
            column.Item().Text("RESUMEN DE PAGO REGISTRADO").FontSize(9).Bold().FontColor("#1E3A8A");
            column.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Background("#2563EB").Padding(4).Text("Concepto").Bold().FontColor(Colors.White);
                    header.Cell().Background("#2563EB").Padding(4).AlignRight().Text("Monto (COP)").Bold().FontColor(Colors.White);
                });

                table.Cell().BorderBottom(1).BorderColor("#EFF6FF").Padding(4).Text("Valor Total de la Venta");
                table.Cell().BorderBottom(1).BorderColor("#EFF6FF").Padding(4).AlignRight().Text($"$ {totalVenta:N0}");

                table.Cell().BorderBottom(1).BorderColor("#EFF6FF").Padding(4).Text($"Monto Abonado en este Recibo ({pago.NumeroComprobante})").Bold();
                table.Cell().BorderBottom(1).BorderColor("#EFF6FF").Padding(4).AlignRight().Text($"$ {pago.Monto:N0}").Bold().FontColor("#16A34A");

                table.Cell().BorderBottom(1).BorderColor("#EFF6FF").Padding(4).Text("Total Acumulado Abonado");
                table.Cell().BorderBottom(1).BorderColor("#EFF6FF").Padding(4).AlignRight().Text($"$ {totalAbonado:N0}");

                table.Cell().BorderBottom(1).BorderColor("#EFF6FF").Background("#FEF2F2").Padding(4).Text("Saldo Restante Pendiente").Bold().FontColor("#DC2626");
                table.Cell().BorderBottom(1).BorderColor("#EFF6FF").Background("#FEF2F2").Padding(4).AlignRight().Text($"$ {saldoRestante:N0}").Bold().FontColor("#DC2626");
            });

            column.Item().PaddingTop(12).Border(1).BorderColor("#93C5FD").Background("#EFF6FF").Padding(6).Column(c =>
            {
                c.Item().Text("Constancia Legal de Pago").FontSize(7.5f).Bold().FontColor("#1E40AF");
                c.Item().Text("Este comprobante certifica el abono recibido. Consérvelo para cualquier reclamo o trámite futuro.").FontSize(7).FontColor("#3B82F6");
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor("#DBEAFE");
            col.Item().PaddingTop(3).Text(x =>
            {
                x.Span("Comprobante generado por ").FontSize(7).FontColor("#6B7280");
                x.Span("LentSoft MVC System").Bold().FontSize(7).FontColor("#2563EB");
                x.Span(" — www.lentsoft.com").FontSize(7).FontColor("#6B7280");
            });
        });
    }
}
