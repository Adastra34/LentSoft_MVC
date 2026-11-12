namespace LentSoft.Web.Services;

public class VentasIndicadoresDto
{
    public decimal VentasDelMes { get; set; }
    public int PedidosActivos { get; set; }
    public int ClientesAtendidos { get; set; }
    public decimal TicketPromedio { get; set; }
}

public class VentasFilterRequest
{
    public int Draw { get; set; } = 1;
    public int Start { get; set; } = 0;
    public int Length { get; set; } = 10;
    public string? SearchValue { get; set; }
    public int SortColumnIndex { get; set; } = 2; // Default fecha
    public string SortDirection { get; set; } = "desc";
    public string? Estado { get; set; }
    public string? EstadoPago { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public string? FiltroRapido { get; set; }
}

public class DataTablesResponse<T>
{
    public int Draw { get; set; }
    public int RecordsTotal { get; set; }
    public int RecordsFiltered { get; set; }
    public List<T> Data { get; set; } = new();
}

public class VentaItemDto
{
    public int ProductId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public string Categoria { get; set; } = string.Empty;
}

public class VentaDataTablesDto
{
    public int Id { get; set; }
    public string NumeroPedido { get; set; } = string.Empty;
    public string ClienteNombre { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Fecha { get; set; } = string.Empty;
    public long FechaTicks { get; set; }
    public decimal Total { get; set; }
    public decimal TotalAbonado { get; set; }
    public decimal SaldoPendiente { get; set; }
    public string EstadoPago { get; set; } = string.Empty;
    public string EstadoOrden { get; set; } = string.Empty;
    public int? FormulaOpticaId { get; set; }
    public bool TieneFormula => FormulaOpticaId.HasValue && FormulaOpticaId.Value > 0;
    public bool RequiereFormula { get; set; }
    public List<VentaItemDto> Items { get; set; } = new();
}

public class VentaItemInputDto
{
    public int ProductId { get; set; }
    public int Cantidad { get; set; }
}

public class RegistrarVentaDto
{
    public int? UserId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? NumeroDocumento { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public int DescuentoPercent { get; set; }
    public string Estado { get; set; } = "pendiente";
    public string MetodoPagoSimulado { get; set; } = "Efectivo";
    public int? FormulaOpticaId { get; set; }
    public string? ItemsJson { get; set; }
    public List<VentaItemInputDto> Items { get; set; } = new();
}

public class ProductoVendidoDto
{
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public int CantidadVendida { get; set; }
    public decimal TotalVendido { get; set; }
}
