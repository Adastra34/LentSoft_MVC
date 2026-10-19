using System.Text.Json.Serialization;

namespace LentSoft.Web.Models.DTOs;

public class DataTablesRequestDTO
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; } = 10;
    public string? SearchValue { get; set; }
    public int SortColumnIndex { get; set; }
    public string? SortDirection { get; set; } // "asc" or "desc"
    public string? Estado { get; set; }
    public string? EstadoPago { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public string? FiltroRapido { get; set; }
}

public class DataTablesResult<T>
{
    [JsonPropertyName("draw")]
    public int Draw { get; set; }

    [JsonPropertyName("recordsTotal")]
    public int RecordsTotal { get; set; }

    [JsonPropertyName("recordsFiltered")]
    public int RecordsFiltered { get; set; }

    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new();
}

public class VentaRowDTO
{
    public int Id { get; set; }
    public string CodigoPedido => $"#ORD-{Id:D4}";
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteDocumento { get; set; } = string.Empty;
    public string ClienteTelefono { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public DateTime FechaPedido { get; set; }
    public string FechaFormato => FechaPedido.ToString("dd/MM/yyyy HH:mm");
    public int TotalItems { get; set; }
    public string ResumenItems { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string TotalFormato => $"COP ${Total:N0}";
    public decimal MontoPagado { get; set; }
    public decimal SaldoPendiente { get; set; }
    public string SaldoFormato => $"COP ${SaldoPendiente:N0}";
    public string Estado { get; set; } = string.Empty;
    public string EstadoPago { get; set; } = string.Empty;
    public string MetodoPago { get; set; } = string.Empty;
    public int? FormulaOpticaId { get; set; }
    public bool TieneFormula => FormulaOpticaId.HasValue && FormulaOpticaId.Value > 0;
    public string FormulaDetalle { get; set; } = string.Empty;
    public bool TieneFactura { get; set; }
    public int? FacturaId { get; set; }
    public string? FacturaNumero { get; set; }
    public string? FacturaEstado { get; set; }
    public int TotalAbonos { get; set; }
}
