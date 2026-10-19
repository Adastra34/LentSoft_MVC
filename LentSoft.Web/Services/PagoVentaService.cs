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

        var totalAbonadoActual = order.Pagos != null && order.Pagos.Any()
            ? order.Pagos.Sum(p => p.Monto)
            : order.MontoPagado;
        var saldoActual = Math.Max(0m, order.Total - totalAbonadoActual);

        if (monto > saldoActual)
        {
            throw new InvalidOperationException($"El monto del abono (${monto:N2}) supera el saldo pendiente (${saldoActual:N2}).");
        }

        // Generar Número de Comprobante único REC-YYYY-XXXX (mayor consecutivo + 1 con reintento)
        var year = DateTime.UtcNow.Year;
        var prefix = $"REC-{year}-";

        var existingComprobantes = await _context.PagosVentas
            .Where(p => p.NumeroComprobante.StartsWith(prefix))
            .Select(p => p.NumeroComprobante)
            .ToListAsync();

        int maxSeq = 0;
        foreach (var comp in existingComprobantes)
        {
            if (comp.Length > prefix.Length && int.TryParse(comp.Substring(prefix.Length), out var seq) && seq > maxSeq)
            {
                maxSeq = seq;
            }
        }

        var pago = new PagoVenta
        {
            VentaId = ventaId,
            Monto = Math.Round(monto, 2),
            MetodoPago = string.IsNullOrWhiteSpace(metodoPago) ? "Efectivo" : metodoPago,
            Responsable = string.IsNullOrWhiteSpace(responsable) ? "Ventas" : responsable,
            FechaPago = DateTime.UtcNow
        };

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

        _context.PagosVentas.Add(pago);

        const int maxRetries = 5;
        for (int retry = 0; retry < maxRetries; retry++)
        {
            var candidateSeq = maxSeq + 1 + retry;
            pago.NumeroComprobante = $"{prefix}{candidateSeq:D4}";

            try
            {
                await _context.SaveChangesAsync();
                break;
            }
            catch (DbUpdateException) when (retry < maxRetries - 1)
            {
                // Reintento en caso de colisión por concurrencia
            }
        }

        return pago;
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
