namespace LentSoft.Web.Models.ViewModels;

public class FacturaResumenDto
{
    public int Id { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaPago { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = "pendiente";
    public int OrderId { get; set; }
}

public class MisFacturasListaViewModel
{
    public List<FacturaResumenDto> Facturas { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 5;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
}

public class FacturaDetalleItemViewModel
{
    public string ProductoNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitarioBase { get; set; }
    public decimal PorcentajeIva { get; set; }
    public decimal ValorIva { get; set; }
    public decimal Subtotal { get; set; }
}

public class FacturaDetalleViewModel
{
    public int Id { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaPago { get; set; }
    public string Estado { get; set; } = "pendiente";
    public string? MetodoPago { get; set; }
    public int OrderId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteDocumento { get; set; } = string.Empty;
    public string? ClienteEmail { get; set; }
    public string? ClienteTelefono { get; set; }
    public string? DireccionEnvio { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Impuestos { get; set; }
    public decimal Total { get; set; }
    public string? CUFE { get; set; }
    public string ResolucionDianNumero { get; set; } = string.Empty;
    public string RangoAutorizadoDesde { get; set; } = string.Empty;
    public string RangoAutorizadoHasta { get; set; } = string.Empty;
    public List<FacturaDetalleItemViewModel> Items { get; set; } = new();
}
