using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class PagoVentaService : IPagoVentaService
{
    private readonly LentSoftDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PagoVentaService> _logger;
    private readonly ILogger _auditLogger;

    public PagoVentaService(
        LentSoftDbContext context,
        IMemoryCache? cache = null,
        ILogger<PagoVentaService>? logger = null,
        ILoggerFactory? loggerFactory = null)
    {
        _context = context;
        _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
        _logger = logger ?? NullLogger<PagoVentaService>.Instance;
        _auditLogger = loggerFactory?.CreateLogger("Auditoria.Ventas") ?? (ILogger)_logger;
    }

    public async Task<PagoVenta> RegistrarAbonoAsync(int ventaId, decimal monto, string metodoPago, string responsable)
    {
        if (monto <= 0)
        {
            throw new ArgumentException("El monto del abono debe ser mayor a cero.");
        }

        var montoRedondeado = Math.Round(monto, 2);
        var metodoNormalizado = string.IsNullOrWhiteSpace(metodoPago) ? "Efectivo" : metodoPago.Trim();
        var responsableNormalizado = string.IsNullOrWhiteSpace(responsable) ? "Ventas" : responsable.Trim();

        // Protección contra cobros/abonos duplicados (ventana de 30 segundos por venta, monto y método)
        var duplicateCacheKey = $"CobroAbono_{ventaId}_{montoRedondeado:0.00}_{metodoNormalizado.ToLowerInvariant()}";
        if (_cache.TryGetValue(duplicateCacheKey, out _))
        {
            _logger.LogWarning(
                "Intento de abono duplicado bloqueado en ventana de 30s para VentaId={VentaId}, Monto={Monto}, Metodo={Metodo}, Responsable={Responsable}",
                ventaId, montoRedondeado, metodoNormalizado, responsableNormalizado);
            throw new InvalidOperationException("Ya se registró un abono idéntico para esta venta en los últimos 30 segundos. Por favor verifique el historial de abonos antes de reintentar.");
        }

        bool ownsTx = _context.Database.CurrentTransaction == null;
        await using var dbTx = ownsTx ? await _context.Database.BeginTransactionAsync() : null;

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

            var totalAbonadoActual = order.Pagos != null && order.Pagos.Any()
                ? order.Pagos.Sum(p => p.Monto)
                : order.MontoPagado;
            var saldoActual = Math.Max(0m, order.Total - totalAbonadoActual);

            if (montoRedondeado > saldoActual)
            {
                throw new InvalidOperationException($"El monto del abono (${montoRedondeado:N2}) supera el saldo pendiente (${saldoActual:N2}).");
            }

            var estadoPagoAnterior = order.EstadoPago;
            var estadoOrdenAnterior = order.Estado;

            // Generar Número de Comprobante único REC-YYYY-XXXX (mayor consecutivo + 1 con reintento)
            var year = DateTime.UtcNow.Year;
            var prefix = $"REC-{year}-";

            var existingComprobantes = await _context.PagosVentas
                .AsNoTracking()
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
                Monto = montoRedondeado,
                MetodoPago = metodoNormalizado,
                Responsable = responsableNormalizado,
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
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }
                catch (DbUpdateException) when (retry < maxRetries - 1)
                {
                    // Reintento en caso de colisión de consecutivo
                }
            }

            if (dbTx != null)
            {
                await dbTx.CommitAsync();
            }

            _cache.Set(duplicateCacheKey, true, TimeSpan.FromSeconds(30));

            _auditLogger.LogInformation(
                "AUDITORIA_VENTAS | Operacion=RegistrarAbono | Entidad=Order#{VentaId}/PagoVenta#{PagoId}({Comprobante}) | Monto={Monto} | MetodoPago={MetodoPago} | EstadoPagoAnterior={EstadoPagoAnterior} | EstadoPagoNuevo={EstadoPagoNuevo} | EstadoOrdenAnterior={EstadoOrdenAnterior} | EstadoOrdenNuevo={EstadoOrdenNuevo} | Responsable={Responsable} | FechaUtc={FechaUtc:O}",
                ventaId, pago.Id, pago.NumeroComprobante, pago.Monto, pago.MetodoPago, estadoPagoAnterior, order.EstadoPago, estadoOrdenAnterior, order.Estado, responsableNormalizado, DateTime.UtcNow);

            return pago;
        }
        catch (DbUpdateConcurrencyException concEx)
        {
            if (dbTx != null)
            {
                await dbTx.RollbackAsync();
            }
            _logger.LogWarning(concEx, "Conflicto de concurrencia al registrar abono en la venta {VentaId}", ventaId);
            throw new InvalidOperationException("El saldo de la venta fue actualizado simultáneamente por otro usuario. Por favor, consulte el saldo actual e intente de nuevo.", concEx);
        }
        catch (Exception ex)
        {
            if (dbTx != null)
            {
                await dbTx.RollbackAsync();
            }
            _logger.LogError(ex, "Error al registrar abono en la venta {VentaId} por {Responsable}", ventaId, responsableNormalizado);
            throw;
        }
    }

    public async Task<List<PagoVenta>> ObtenerAbonosPorVentaAsync(int ventaId)
    {
        return await _context.PagosVentas
            .AsNoTracking()
            .Where(p => p.VentaId == ventaId)
            .OrderByDescending(p => p.FechaPago)
            .ToListAsync();
    }

    public async Task<PagoVenta?> ObtenerPagoPorIdAsync(int pagoId)
    {
        return await _context.PagosVentas
            .AsNoTracking()
            .Include(p => p.Venta)
                .ThenInclude(v => v!.User)
            .FirstOrDefaultAsync(p => p.Id == pagoId);
    }
}
