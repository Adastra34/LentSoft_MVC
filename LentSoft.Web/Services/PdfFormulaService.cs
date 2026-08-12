using System;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class PdfFormulaService : IPdfFormulaService
{
    public PdfFormulaService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateFormulaPdf(FormulaOptica formula)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Header().Element(headerContainer => ComposeHeader(headerContainer, formula));
                page.Content().Element(contentContainer => ComposeContent(contentContainer, formula));
                page.Footer().Element(footerContainer => ComposeFooter(footerContainer, formula));
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, FormulaOptica formula)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("LENTSOFT OPTOMETRÍA").FontSize(18).Bold().FontColor("#4C1D95");
                    column.Item().Text("Clínica de Especialidades Visuales").FontSize(9).Bold().FontColor("#374151");
                    column.Item().Text("Dirección: Calle Principal # 12-34, Bogotá D.C.").FontSize(8).FontColor("#6B7280");
                    column.Item().Text("PBX: +57 (601) 555-0199 | Correo: contacto@lentsoft.com").FontSize(8).FontColor("#6B7280");
                });

                row.ConstantItem(200).Column(column =>
                {
                    column.Item().Background("#7C3AED").Padding(6).Text("FÓRMULA DE OPTOMETRÍA")
                        .FontSize(11).Bold().AlignCenter().FontColor(Colors.White);

                    column.Item().Border(1).BorderColor("#7C3AED").Padding(6).Column(c =>
                    {
                        c.Item().Text($"Receta N°: FOR-{formula.Id:D4}").FontSize(10).Bold().FontColor("#4C1D95");
                        c.Item().Text($"Fecha Emisión: {formula.Fecha.ToLocalTime():dd/MM/yyyy}").FontSize(8);
                        c.Item().Text($"Estado: {formula.Estado.ToUpper()}").FontSize(8).Bold().FontColor(formula.Estado == "Vigente" ? "#10B981" : "#EF4444");
                    });
                });
            });
            col.Item().PaddingTop(10).LineHorizontal(1).LineColor("#E9D5FF");
        });
    }

    private void ComposeContent(IContainer container, FormulaOptica formula)
    {
        container.PaddingVertical(10).Column(column =>
        {
            // Información del Paciente
            column.Item().Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor("#E9D5FF").Padding(8).Column(col =>
                {
                    col.Item().PaddingBottom(2).Text("DATOS DEL PACIENTE").FontSize(9).Bold().FontColor("#4C1D95");
                    col.Item().Text($"Nombre: {formula.User?.NombreCompleto ?? "N/A"}").Bold();
                    col.Item().Text($"Identificación: {formula.User?.TipoDocumento} {formula.User?.NumeroDocumento}");
                    col.Item().Text($"Email: {formula.User?.Email ?? "N/A"}");
                    col.Item().Text($"Teléfono: {formula.User?.Telefono ?? "N/A"}");
                });

                row.ConstantItem(15);

                row.RelativeItem().Border(1).BorderColor("#E9D5FF").Padding(8).Column(col =>
                {
                    col.Item().PaddingBottom(2).Text("DETALLES DE LA RECETA").FontSize(9).Bold().FontColor("#4C1D95");
                    col.Item().Text($"Tipo de Lente: {formula.TipoLente}").Bold();
                    col.Item().Text($"Distancia Pupilar (DP): {formula.DistanciaPupilar ?? "N/A"} mm");
                    col.Item().Text($"Fecha Vencimiento: {formula.Fecha.AddMonths(12).ToLocalTime():dd/MM/yyyy}");
                });
            });

            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor("#E9D5FF");

            // Grilla de Corrección Visual
            column.Item().PaddingBottom(4).Text("FÓRMULA CORRECTORA").FontSize(9).Bold().FontColor("#4C1D95");
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2); // Ojo
                    columns.RelativeColumn(1.5f); // Esfera
                    columns.RelativeColumn(1.5f); // Cilindro
                    columns.RelativeColumn(1.5f); // Eje
                });

                table.Header(header =>
                {
                    header.Cell().Background("#7C3AED").Padding(5).Text("Ojo").Bold().FontColor(Colors.White);
                    header.Cell().Background("#7C3AED").Padding(5).AlignCenter().Text("Esfera (SPH)").Bold().FontColor(Colors.White);
                    header.Cell().Background("#7C3AED").Padding(5).AlignCenter().Text("Cilindro (CYL)").Bold().FontColor(Colors.White);
                    header.Cell().Background("#7C3AED").Padding(5).AlignCenter().Text("Eje (AXIS)").Bold().FontColor(Colors.White);
                });

                // Fila Ojo Derecho
                table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).Text("Ojo Derecho (OD)").Bold();
                table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignCenter().Text(formula.EsferaOD);
                table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignCenter().Text(formula.CilindroOD);
                table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignCenter().Text(formula.EjeOD);

                // Fila Ojo Izquierdo
                table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).Text("Ojo Izquierdo (OI)").Bold();
                table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignCenter().Text(formula.EsferaOI);
                table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignCenter().Text(formula.CilindroOI);
                table.Cell().BorderBottom(1).BorderColor("#F3E8FF").Padding(5).AlignCenter().Text(formula.EjeOI);
            });

            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor("#E9D5FF");

            // Observaciones / Indicaciones
            column.Item().Column(obsCol =>
            {
                obsCol.Item().PaddingBottom(2).Text("OBSERVACIONES Y RECOMENDACIONES CLÍNICAS").FontSize(9).Bold().FontColor("#4C1D95");
                obsCol.Item().Background("#F9F5FF").Padding(8).Text(formula.Observaciones ?? "Sin observaciones adicionales.").Italic().FontColor("#374151");
            });
        });
    }

    private void ComposeFooter(IContainer container, FormulaOptica formula)
    {
        container.Column(col =>
        {
            // Firmas
            col.Item().PaddingBottom(20).Row(row =>
            {
                row.RelativeItem(); // Espacio vacío a la izquierda

                row.ConstantItem(200).Column(signature2 =>
                {
                    signature2.Item().AlignCenter().Text(formula.Optometra?.NombreCompleto ?? "Dr(a). Especialista").Bold().FontSize(9);
                    signature2.Item().AlignCenter().Text("Optómetra Especialista").FontSize(8).FontColor("#6B7280");
                    signature2.Item().AlignCenter().Text($"Registro Médico: {formula.Optometra?.RegistroMedico ?? "N/A"}").FontSize(8).FontColor("#6B7280");
                    signature2.Item().LineHorizontal(1).LineColor("#6B7280");
                    signature2.Item().AlignCenter().Text("Firma del Optómetra").FontSize(8);
                });
            });

            col.Item().LineHorizontal(1).LineColor("#E9D5FF");
            col.Item().PaddingTop(4).Text(x =>
            {
                x.Span("Receta óptica emitida por software clínico autorizado ").FontSize(7.5f).FontColor("#6B7280");
                x.Span("LentSoft Optometría").Bold().FontSize(7.5f).FontColor("#7C3AED");
                x.Span(" — www.lentsoft.com. Vigencia de la fórmula: 12 meses a partir de la emisión.").FontSize(7.5f).FontColor("#6B7280");
            });
        });
    }
}
