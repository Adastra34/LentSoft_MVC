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
        // 0. Si viene con VentaId, validar ANTES de cobrar que la venta exista, esté activa y que el monto no supere el saldo
        if (dto.VentaId.HasValue && dto.VentaId.Value > 0)
        {
            var order = await _context.Orders
                .Include(o => o.Pagos)
                .FirstOrDefaultAsync(o => o.Id == dto.VentaId.Value);

            if (order == null || !order.Activo)
            {
                return GenerarResultadoFallido(dto, "No se encontró la venta especificada o se encuentra inactiva.");
            }

            if (order.Estado == "cancelado")
            {
                return GenerarResultadoFallido(dto, "No se pueden realizar cobros sobre una venta cancelada.");
            }

            var totalAbonado = order.Pagos != null && order.Pagos.Any()
                ? order.Pagos.Sum(p => p.Monto)
                : order.MontoPagado;
            var saldoActual = Math.Max(0m, order.Total - totalAbonado);

            if (dto.Monto <= 0)
            {
                return GenerarResultadoFallido(dto, "El monto a cobrar debe ser mayor a cero.");
            }

            if (dto.Monto > saldoActual)
            {
                return GenerarResultadoFallido(dto, $"El monto a cobrar (${dto.Monto:N2}) supera el saldo pendiente (${saldoActual:N2}).");
            }
        }
        else if (dto.Monto <= 0)
        {
            return GenerarResultadoFallido(dto, "El monto a cobrar debe ser mayor a cero.");
        }

        var numeroLimpio = dto.NumeroTarjeta?.Replace(" ", "").Replace("-", "").Trim() ?? string.Empty;

        // 1. Validaciones básicas de tarjeta (nunca se loguea ni persiste el número completo ni CVV)
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

        var marca = DetectarMarcaTarjeta(numeroLimpio);
        var ultimosDigitos = numeroLimpio.Substring(Math.Max(0, numeroLimpio.Length - 4));

        // 2. Simulación de aprobación/rechazo
        // Tarjeta terminada en '9' se rechaza por simulación bancaria
        bool esAprobada = !numeroLimpio.EndsWith("9");
        string motivoRechazo = esAprobada ? string.Empty : "Transacción rechazada por el banco emisor (Fondos Insuficientes / Tarjeta de prueba rechazada).";
        var codigoAutorizacion = esAprobada ? $"AUTH-{Random.Shared.Next(100000, 999999)}" : null;

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

        // 3. Ejecución atómica en una sola transacción de base de datos
        using var dbTx = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.TransaccionesPagos.Add(transaccion);
            await _context.SaveChangesAsync();

            if (!esAprobada)
            {
                await dbTx.CommitAsync();
                return new ResultadoTransaccionDTO
                {
                    Exitoso = false,
                    Mensaje = motivoRechazo,
                    Transaccion = transaccion
                };
            }

            PagoVenta? pagoRegistrado = null;
            if (dto.VentaId.HasValue && dto.VentaId.Value > 0)
            {
                pagoRegistrado = await _pagoVentaService.RegistrarAbonoAsync(
                    dto.VentaId.Value, 
                    dto.Monto, 
                    $"Tarjeta {marca} (****{ultimosDigitos})", 
                    dto.Responsable
                );
            }

            await dbTx.CommitAsync();

            return new ResultadoTransaccionDTO
            {
                Exitoso = true,
                Mensaje = $"Pago aprobado. Código de Autorización: {codigoAutorizacion}",
                Transaccion = transaccion,
                PagoRegistrado = pagoRegistrado
            };
        }
        catch (Exception)
        {
            await dbTx.RollbackAsync();
            throw;
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

    private static ResultadoTransaccionDTO GenerarResultadoFallido(TarjetaPagoDTO dto, string errorMsg)
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
                MensajeRespuesta = errorMsg,
                Fecha = DateTime.UtcNow
            }
        };
    }
}
