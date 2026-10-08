using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class PdfRecetaService : IPdfRecetaService
{
    public PdfRecetaService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateRecetaPdf(FormulaOptica formula)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Content().Element(contentContainer => ComposeRecipe(contentContainer, formula));
                page.Footer().Element(footerContainer => ComposeFooter(footerContainer));
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeRecipe(IContainer container, FormulaOptica formula)
    {
        container.Border(2).BorderColor("#7C3AED").Padding(20).Column(col =>
        {
            // Encabezado
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(headerCol =>
                {
                    headerCol.Item().Text("LentSoft Optometría").FontSize(20).Bold().FontColor("#4C1D95");
                    headerCol.Item().Text("Clínica de Especialidades Visuales").FontSize(9).FontColor("#6B7280");
                });

                row.ConstantItem(180).Column(headerRight =>
                {
                    headerRight.Item().AlignRight().Text("RECETA CLÍNICA").FontSize(13).Bold().FontColor("#7C3AED");
                    headerRight.Item().AlignRight().Text($"Fórmula #FOR-{formula.Id}").FontSize(10).FontColor("#6B7280");
                });
            });

            col.Item().PaddingTop(12).PaddingBottom(14).LineHorizontal(2).LineColor("#7C3AED");

            // Información del Paciente
            col.Item().Background("#F5F3FF").Border(1).BorderColor("#DDD6FE").Padding(12).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    var nombre = formula.User?.NombreCompleto ?? "N/A";
                    c.Item().Text(t =>
                    {
                        t.Span("Paciente: ").Bold().FontColor("#374151");
                        t.Span(nombre).FontColor("#111827");
                    });

                    var docTipo = formula.User?.TipoDocumento ?? "CC";
                    var docNum = formula.User?.NumeroDocumento ?? "N/A";
                    c.Item().PaddingTop(4).Text(t =>
                    {
                        t.Span("Identificación: ").Bold().FontColor("#374151");
                        t.Span($"{docTipo}: {docNum}").FontColor("#111827");
                    });
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.Span("Fecha Emisión: ").Bold().FontColor("#374151");
                        t.Span(formula.Fecha.ToString("dd/MM/yyyy")).FontColor("#111827");
                    });

                    c.Item().PaddingTop(4).Text(t =>
                    {
                        t.Span("Tipo Lente: ").Bold().FontColor("#374151");
                        t.Span(formula.TipoLente ?? "N/A").Bold().FontColor("#4C1D95");
                    });
                });
            });

            col.Item().PaddingVertical(14);

            // Grilla de Corrección Visual
            col.Item().Table(table =>
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
                    header.Cell().Background("#7C3AED").Border(1).BorderColor("#DDD6FE").Padding(7).Text("Ojo").Bold().FontColor(Colors.White);
                    header.Cell().Background("#7C3AED").Border(1).BorderColor("#DDD6FE").Padding(7).AlignCenter().Text("Esfera (SPH)").Bold().FontColor(Colors.White);
                    header.Cell().Background("#7C3AED").Border(1).BorderColor("#DDD6FE").Padding(7).AlignCenter().Text("Cilindro (CYL)").Bold().FontColor(Colors.White);
                    header.Cell().Background("#7C3AED").Border(1).BorderColor("#DDD6FE").Padding(7).AlignCenter().Text("Eje (AXIS)").Bold().FontColor(Colors.White);
                });

                // Ojo Derecho (OD)
                table.Cell().Border(1).BorderColor("#DDD6FE").Padding(7).Text("Ojo Derecho (OD)").Bold().FontColor("#374151");
                table.Cell().Border(1).BorderColor("#DDD6FE").Padding(7).AlignCenter().Text(formula.EsferaOD ?? "0.00");
                table.Cell().Border(1).BorderColor("#DDD6FE").Padding(7).AlignCenter().Text(formula.CilindroOD ?? "0.00");
                table.Cell().Border(1).BorderColor("#DDD6FE").Padding(7).AlignCenter().Text(formula.EjeOD ?? "0");

                // Ojo Izquierdo (OI)
                table.Cell().Border(1).BorderColor("#DDD6FE").Padding(7).Text("Ojo Izquierdo (OI)").Bold().FontColor("#374151");
                table.Cell().Border(1).BorderColor("#DDD6FE").Padding(7).AlignCenter().Text(formula.EsferaOI ?? "0.00");
                table.Cell().Border(1).BorderColor("#DDD6FE").Padding(7).AlignCenter().Text(formula.CilindroOI ?? "0.00");
                table.Cell().Border(1).BorderColor("#DDD6FE").Padding(7).AlignCenter().Text(formula.EjeOI ?? "0");
            });

            // Distancia Pupilar y Estado
            col.Item().PaddingTop(12).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Distancia Pupilar (DP): ").Bold().FontColor("#374151");
                    t.Span($"{(string.IsNullOrEmpty(formula.DistanciaPupilar) ? "N/A" : formula.DistanciaPupilar)} mm").Bold().FontColor("#111827");
                });

                var estadoColor = formula.Estado.Equals("Vigente", StringComparison.OrdinalIgnoreCase) ? "#059669" : "#DC2626";
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("Estado de Validez: ").Bold().FontColor("#374151");
                    t.Span(formula.Estado).Bold().FontColor(estadoColor);
                });
            });

            // Observaciones
            col.Item().PaddingTop(16).LineHorizontal(1).LineColor("#DDD6FE");
            col.Item().PaddingTop(8).Column(obsCol =>
            {
                obsCol.Item().Text("Observaciones y Recomendaciones:").Bold().FontColor("#374151");
                var obs = string.IsNullOrWhiteSpace(formula.Observaciones) ? "Ninguna." : formula.Observaciones;
                obsCol.Item().PaddingTop(3).Text(obs).Italic().FontColor("#4B5563");
            });

            // Firmas
            col.Item().PaddingTop(40).Row(row =>
            {
                row.RelativeItem().Column(sigCol =>
                {
                    sigCol.Item().PaddingTop(30).LineHorizontal(1).LineColor("#9CA3AF");
                    sigCol.Item().PaddingTop(4).AlignCenter().Text("Firma del Paciente").FontSize(9).FontColor("#4B5563");
                });

                row.ConstantItem(40); // Espacio entre firmas

                row.RelativeItem().Column(sigCol =>
                {
                    var optoNombre = formula.Optometra?.NombreCompleto ?? "Optómetra Especialista";
                    var rm = formula.Optometra?.RegistroMedico ?? "N/A";
                    sigCol.Item().PaddingTop(10).AlignCenter().Text(optoNombre).Bold().FontSize(10).FontColor("#111827");
                    sigCol.Item().AlignCenter().Text("Optómetra Especialista").FontSize(8.5f).FontColor("#6B7280");
                    sigCol.Item().AlignCenter().Text($"R.M. {rm}").FontSize(8.5f).FontColor("#6B7280");
                    sigCol.Item().PaddingTop(4).LineHorizontal(1).LineColor("#9CA3AF");
                    sigCol.Item().PaddingTop(4).AlignCenter().Text("Firma y Sello del Especialista").FontSize(9).FontColor("#4B5563");
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text("Documento médico expedido por LentSoft Clínica de Especialidades Visuales. Válido como prescripción médica óptica.")
            .FontSize(7.5f).FontColor("#9CA3AF");
    }
}
