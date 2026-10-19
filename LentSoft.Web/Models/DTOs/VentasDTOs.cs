using System.ComponentModel.DataAnnotations;

namespace LentSoft.Web.Models.DTOs;

public class RegistrarVentaDTO
{
    public int? UserId { get; set; }

    [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido del cliente es obligatorio.")]
    public string Apellido { get; set; } = string.Empty;

    public string? NumeroDocumento { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }

    [Range(0, 30, ErrorMessage = "El descuento debe estar entre 0% y 30%.")]
    public int DescuentoPercent { get; set; }

    public string Estado { get; set; } = "pendiente";
    public string MetodoPagoSimulado { get; set; } = "Efectivo";
    public string? ItemsJson { get; set; }
    public int? FormulaOpticaId { get; set; }
}

public class ItemVentaInputDTO
{
    public int ProductId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class ProductoVendidoReporteDTO
{
    public int ProductId { get; set; }
    public string Producto { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int CantidadTotal { get; set; }
    public decimal TotalVentas { get; set; }
}

public class FormulaOpticaVigenteDTO
{
    public bool Encontrado { get; set; }
    public bool EsVigente { get; set; }
    public string? Mensaje { get; set; }
    public FormulaOpticaDetalleDTO? Formula { get; set; }
}

public class FormulaOpticaDetalleDTO
{
    public int Id { get; set; }
    public string Fecha { get; set; } = string.Empty;
    public string FechaFormato { get; set; } = string.Empty;
    public string EsferaOD { get; set; } = string.Empty;
    public string CilindroOD { get; set; } = string.Empty;
    public string EjeOD { get; set; } = string.Empty;
    public string EsferaOI { get; set; } = string.Empty;
    public string CilindroOI { get; set; } = string.Empty;
    public string EjeOI { get; set; } = string.Empty;
    public string TipoLente { get; set; } = string.Empty;
    public string DistanciaPupilar { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

public class FormulaOpticaResumenDTO
{
    public int Id { get; set; }
    public string Fecha { get; set; } = string.Empty;
    public string TipoLente { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public bool EsVigente { get; set; }
}
