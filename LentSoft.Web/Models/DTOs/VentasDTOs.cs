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

public class CrearPedidoVentaDTO
{
    public string? NumeroPedido { get; set; }

    [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre del cliente no puede superar los 150 caracteres.")]
    public string ClienteNombre { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un producto válido del catálogo.")]
    public int ProductId { get; set; }

    [Range(1, 10000, ErrorMessage = "La cantidad debe estar entre 1 y 10,000.")]
    public int Cantidad { get; set; } = 1;

    [Required(ErrorMessage = "El estado del pedido es obligatorio.")]
    public string Estado { get; set; } = "Pendiente";

    [StringLength(500, ErrorMessage = "Las notas no pueden superar los 500 caracteres.")]
    public string? Notas { get; set; }
}

public class EditarPedidoVentaDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "Identificador de pedido inválido.")]
    public int Id { get; set; }

    [Required(ErrorMessage = "El número de pedido es obligatorio.")]
    [StringLength(50)]
    public string NumeroPedido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
    [StringLength(150)]
    public string ClienteNombre { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un producto válido del catálogo.")]
    public int ProductId { get; set; }

    [Range(1, 10000, ErrorMessage = "La cantidad debe estar entre 1 y 10,000.")]
    public int Cantidad { get; set; } = 1;

    [Required(ErrorMessage = "El estado del pedido es obligatorio.")]
    public string Estado { get; set; } = "Pendiente";

    [StringLength(500)]
    public string? Notas { get; set; }
}

public class ResultadoOperacionPedidoDTO
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string? Advertencia { get; set; }
    public int? PedidoId { get; set; }
}
