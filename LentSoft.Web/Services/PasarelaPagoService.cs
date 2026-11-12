using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class PasarelaPagoService : IPasarelaPagoService
{
    private readonly LentSoftDbContext _context;
    private readonly IPagoVentaService _pagoVentaService;

    public PasarelaPagoService(LentSoftDbContext context, IPagoVentaService pagoVentaService)
    {
        _context = context;
        _pagoVentaService = pagoVentaService;
    }

    public async Task<ResultadoTransaccionDTO> ProcesarPagoTarjetaAsync(TarjetaPagoDTO dto)
    {
        var numeroLimpio = dto.NumeroTarjeta?.Replace(" ", "").Replace("-", "").Trim() ?? string.Empty;

        // 1. Validaciones básicas de tarjeta
        if (string.IsNullOrWhiteSpace(numeroLimpio) || numeroLimpio.Length < 13 || numeroLimpio.Length > 19 || !numeroLimpio.All(char.IsDigit))
        {
            return GenerarResultadoFallido(dto, "Número de tarjeta inválido. Debe contener entre 13 y 19 dígitos numéricos.");
        }

        if (!ValidarAlgoritmoLuhn(numeroLimpio))
        {
            return GenerarResultadoFallido(dto, "El número de tarjeta no supera la validación del algoritmo de Luhn.");
        }

        if (string.IsNullOrWhiteSpace(dto.NombreTitular) || dto.NombreTitular.Trim().Length < 3)
        {
            return GenerarResultadoFallido(dto, "Nombre del titular no válido.");
        }

        if (!ValidarFechaExpiracion(dto.FechaExpiracion))
        {
            return GenerarResultadoFallido(dto, "La tarjeta de crédito está vencida o el formato de fecha es incorrecto (MM/YY).");
        }

        if (string.IsNullOrWhiteSpace(dto.Cvv) || (dto.Cvv.Length != 3 && dto.Cvv.Length != 4) || !dto.Cvv.All(char.IsDigit))
        {
            return GenerarResultadoFallido(dto, "Código CVV inválido (debe tener 3 o 4 dígitos).");
        }

        if (dto.Monto <= 0)
        {
            return GenerarResultadoFallido(dto, "El monto a cobrar debe ser mayor a cero.");
        }

        var marca = DetectarMarcaTarjeta(numeroLimpio);
        var ultimosDigitos = numeroLimpio.Substring(Math.Max(0, numeroLimpio.Length - 4));

        // 2. Validación de venta asociada ANTES de procesar cobro
        if (dto.VentaId.HasValue && dto.VentaId.Value > 0)
        {
            var order = await _context.Orders
                .Include(o => o.Pagos)
                .FirstOrDefaultAsync(o => o.Id == dto.VentaId.Value);

            if (order == null || !order.Activo)
            {
                return GenerarResultadoFallido(dto, "La venta o pedido especificado no existe o se encuentra inactivo.", marca, ultimosDigitos);
            }

            if (string.Equals(order.Estado, "cancelado", StringComparison.OrdinalIgnoreCase))
            {
                return GenerarResultadoFallido(dto, "No se puede procesar cobros sobre un pedido cancelado.", marca, ultimosDigitos);
            }

            var saldoPendiente = order.SaldoPendiente;
            if (dto.Monto > saldoPendiente)
            {
                return GenerarResultadoFallido(dto, $"El monto a cobrar (${dto.Monto:N2}) supera el saldo pendiente de la venta (${saldoPendiente:N2}).", marca, ultimosDigitos);
            }
        }

        // 3. Simulación de aprobación/rechazo (tarjeta terminada en 9 = rechazada)
        bool esAprobada = !numeroLimpio.EndsWith("9");
        string motivoRechazo = esAprobada ? string.Empty : "Transacción rechazada por el banco emisor (Fondos Insuficientes / Tarjeta de prueba rechazada).";
        var codigoAutorizacion = esAprobada ? $"AUTH-{Random.Shared.Next(100000, 999999)}" : null;

        // 4. Guardar TransaccionPago y PagoVenta en una sola transacción atómica de base de datos
        var hasExternalTx = _context.Database.CurrentTransaction != null;
        var transaction = hasExternalTx ? null : await _context.Database.BeginTransactionAsync();

        try
        {
            var transaccion = new TransaccionPago
            {
                VentaId = dto.VentaId,
                Monto = Math.Round(dto.Monto, 2),
                Estado = esAprobada ? "Aprobada" : "Rechazada",
                CodigoAutorizacion = codigoAutorizacion,
                UltimosDigitosTarjeta = ultimosDigitos,
                MarcaTarjeta = marca,
                MensajeRespuesta = esAprobada ? "Transacción Aprobada Exitosamente" : motivoRechazo,
                Fecha = DateTime.UtcNow
            };

            _context.TransaccionesPagos.Add(transaccion);
            await _context.SaveChangesAsync();

            PagoVenta? pagoRegistrado = null;
            if (esAprobada && dto.VentaId.HasValue && dto.VentaId.Value > 0)
            {
                pagoRegistrado = await _pagoVentaService.RegistrarAbonoAsync(
                    dto.VentaId.Value, 
                    dto.Monto, 
                    $"Tarjeta {marca} (****{ultimosDigitos})", 
                    dto.Responsable
                );
            }

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            return new ResultadoTransaccionDTO
            {
                Exitoso = esAprobada,
                Mensaje = esAprobada ? $"Pago aprobado. Código de Autorización: {codigoAutorizacion}" : motivoRechazo,
                Transaccion = transaccion,
                PagoRegistrado = pagoRegistrado
            };
        }
        catch (Exception)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public bool ValidarAlgoritmoLuhn(string numeroTarjeta)
    {
        if (string.IsNullOrWhiteSpace(numeroTarjeta)) return false;

        int sum = 0;
        bool alternate = false;

        for (int i = numeroTarjeta.Length - 1; i >= 0; i--)
        {
            int digit = numeroTarjeta[i] - '0';
            if (alternate)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
            alternate = !alternate;
        }

        return (sum % 10 == 0);
    }

    public string DetectarMarcaTarjeta(string numeroTarjeta)
    {
        if (string.IsNullOrWhiteSpace(numeroTarjeta)) return "Desconocida";

        if (numeroTarjeta.StartsWith("4")) return "Visa";
        if (numeroTarjeta.StartsWith("51") || numeroTarjeta.StartsWith("52") || numeroTarjeta.StartsWith("53") || numeroTarjeta.StartsWith("54") || numeroTarjeta.StartsWith("55")) return "Mastercard";
        if (numeroTarjeta.StartsWith("34") || numeroTarjeta.StartsWith("37")) return "American Express";
        if (numeroTarjeta.StartsWith("6011") || numeroTarjeta.StartsWith("65")) return "Discover";

        return "Tarjeta";
    }

    private bool ValidarFechaExpiracion(string fechaExpiracion)
    {
        if (string.IsNullOrWhiteSpace(fechaExpiracion)) return false;

        var parts = fechaExpiracion.Split('/');
        if (parts.Length != 2) return false;

        if (!int.TryParse(parts[0].Trim(), out int mes) || mes < 1 || mes > 12) return false;
        if (!int.TryParse(parts[1].Trim(), out int anioShort)) return false;

        int anio = 2000 + anioShort;
        var now = DateTime.UtcNow;

        var lastDayOfMonth = DateTime.DaysInMonth(anio, mes);
        var expDate = new DateTime(anio, mes, lastDayOfMonth, 23, 59, 59, DateTimeKind.Utc);

        return expDate >= now;
    }

    private static ResultadoTransaccionDTO GenerarResultadoFallido(TarjetaPagoDTO dto, string errorMsg, string? marca = null, string? ultimosDigitos = null)
    {
        return new ResultadoTransaccionDTO
        {
            Exitoso = false,
            Mensaje = errorMsg,
            Transaccion = new TransaccionPago
            {
                VentaId = dto.VentaId,
                Monto = dto.Monto,
                Estado = "Rechazada",
                MarcaTarjeta = marca,
                UltimosDigitosTarjeta = ultimosDigitos,
                MensajeRespuesta = errorMsg,
                Fecha = DateTime.UtcNow
            }
        };
    }
}
