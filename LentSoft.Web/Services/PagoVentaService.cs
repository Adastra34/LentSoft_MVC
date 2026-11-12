using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class PagoVentaService : IPagoVentaService
{
    private readonly LentSoftDbContext _context;

    public PagoVentaService(LentSoftDbContext context)
    {
        _context = context;
    }

    public async Task<PagoVenta> RegistrarAbonoAsync(int ventaId, decimal monto, string metodoPago, string responsable)
    {
        if (monto <= 0)
        {
            throw new ArgumentException("El monto del abono debe ser mayor a cero.");
        }

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

        // Generar Número de Comprobante único REC-YYYY-XXXX (mayor consecutivo + 1 con reintento)
        var year = DateTime.UtcNow.Year;
        var prefix = $"REC-{year}-";
        const int maxIntentos = 5;

        for (int intento = 1; intento <= maxIntentos; intento++)
        {
            var hasExternalTx = _context.Database.CurrentTransaction != null;
            var transaction = hasExternalTx ? null : await _context.Database.BeginTransactionAsync();

            try
            {
                var recibosDelAnio = await _context.PagosVentas
                    .Where(p => p.NumeroComprobante.StartsWith(prefix))
                    .Select(p => p.NumeroComprobante)
                    .ToListAsync();

                int maxConsecutivo = 0;
                foreach (var rec in recibosDelAnio)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(rec, @"(\d+)$");
                    if (match.Success && int.TryParse(match.Value, out int seq))
                    {
                        if (seq > maxConsecutivo) maxConsecutivo = seq;
                    }
                }

                int siguiente = maxConsecutivo + 1;
                var candidate = $"REC-{year}-{siguiente:D4}";

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

                await _context.SaveChangesAsync();

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                return pago;
            }
            catch (DbUpdateException ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }

                if (intento >= maxIntentos)
                {
                    throw new InvalidOperationException("No se pudo generar un número de comprobante único para el abono tras múltiples intentos por concurrencia.", ex);
                }

                await Task.Delay(50 * intento);
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        throw new InvalidOperationException("Error inesperado al registrar el comprobante de abono.");
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
