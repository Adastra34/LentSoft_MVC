using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class PagoVentaService : IPagoVentaService
{
    private readonly LentSoftDbContext _context;
    private readonly ILogger<PagoVentaService> _logger;

    public PagoVentaService(LentSoftDbContext context, ILogger<PagoVentaService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagoVenta> RegistrarAbonoAsync(int ventaId, decimal monto, string metodoPago, string responsable)
    {
        if (monto <= 0)
        {
            throw new ArgumentException("El monto del abono debe ser mayor a cero.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.Orders
                .Include(o => o.Pagos)
                .FirstOrDefaultAsync(o => o.Id == ventaId);

            if (order == null || !order.Activo)
            {
                throw new KeyNotFoundException("No se encontró la venta o pedido especificado.");
            }

            if (order.Estado == "cancelado")
            {
                throw new InvalidOperationException("No se pueden registrar abonos sobre una venta cancelada.");
            }

            var totalAbonadoActual = order.Pagos.Sum(p => p.Monto);
            var saldoActual = Math.Max(0m, order.Total - totalAbonadoActual);

            if (monto > saldoActual)
            {
                throw new InvalidOperationException($"El monto del abono (${monto:N2}) supera el saldo pendiente (${saldoActual:N2}).");
            }

            // Generar Número de Comprobante único REC-YYYY-XXXX
            var year = DateTime.UtcNow.Year;
            var count = (await _context.PagosVentas.CountAsync()) + 1;
            var candidate = $"REC-{year}-{count:D4}";

            while (await _context.PagosVentas.AnyAsync(p => p.NumeroComprobante == candidate))
            {
                count++;
                candidate = $"REC-{year}-{count:D4}";
            }

            var pago = new PagoVenta
            {
                VentaId = ventaId,
                Monto = Math.Round(monto, 2),
                MetodoPago = string.IsNullOrWhiteSpace(metodoPago) ? "Efectivo" : metodoPago,
                Responsable = string.IsNullOrWhiteSpace(responsable) ? "Ventas" : responsable,
                FechaPago = DateTime.UtcNow,
                NumeroComprobante = candidate
            };

            _context.PagosVentas.Add(pago);

            // Actualizar datos calculados en Order
            var estadoPagoAnterior = order.EstadoPago;
            var nuevoTotalAbonado = totalAbonadoActual + pago.Monto;
            order.MontoPagado = nuevoTotalAbonado;

            if (nuevoTotalAbonado >= order.Total)
            {
                order.EstadoPago = "pagado";
                order.Estado = "pagado";
            }
            else
            {
                order.EstadoPago = "abonado";
            }

            // Registrar Auditoría
            var audit = new AuditoriaVenta
            {
                TipoEntidad = "PagoVenta",
                EntidadId = ventaId,
                EstadoAnterior = estadoPagoAnterior,
                EstadoNuevo = order.EstadoPago,
                Accion = "AbonoRegistrado",
                Usuario = responsable,
                Fecha = DateTime.UtcNow,
                Detalles = $"Abono de {pago.Monto:C} con {pago.MetodoPago}. Comprobante: {pago.NumeroComprobante}. Saldo restante: {order.SaldoPendiente:C}"
            };
            _context.AuditoriasVentas.Add(audit);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Abono {Comprobante} por valor de {Monto:C} registrado exitosamente para la orden #{OrderId} por {Responsable}.", pago.NumeroComprobante, pago.Monto, ventaId, responsable);
            return pago;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error de concurrencia al registrar abono para orden #{OrderId}", ventaId);
            throw new InvalidOperationException("La orden fue actualizada por otro usuario simultáneamente. Por favor intente de nuevo.", ex);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error al registrar abono para orden #{OrderId}", ventaId);
            throw;
        }
    }

    public async Task<List<PagoVenta>> ObtenerAbonosPorVentaAsync(int ventaId)
    {
        return await _context.PagosVentas
            .Where(p => p.VentaId == ventaId)
            .OrderByDescending(p => p.FechaPago)
            .ToListAsync();
    }

    public async Task<PagoVenta?> ObtenerPagoPorIdAsync(int pagoId)
    {
        return await _context.PagosVentas
            .Include(p => p.Venta)
                .ThenInclude(v => v!.User)
            .FirstOrDefaultAsync(p => p.Id == pagoId);
    }
}
