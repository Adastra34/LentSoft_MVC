using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

/// <summary>
/// Implementación de IPasarelaPagoService para Wompi Colombia (Modo Sandbox).
/// </summary>
public class WompiPasarelaService : IPasarelaPagoService
{
    private readonly LentSoftDbContext _context;
    private readonly IPagoVentaService _pagoVentaService;
    private readonly ILogger<WompiPasarelaService> _logger;
    private readonly string _publicKey;

    public WompiPasarelaService(
        LentSoftDbContext context,
        IPagoVentaService pagoVentaService,
        IConfiguration configuration,
        ILogger<WompiPasarelaService> logger)
    {
        _context = context;
        _pagoVentaService = pagoVentaService;
        _logger = logger;
        _publicKey = configuration["Pasarela:WompiPublicKey"] ?? "pub_test_wompi_sandbox_token";
    }

    public async Task<ResultadoTransaccionDTO> ProcesarPagoTarjetaAsync(TarjetaPagoDTO dto)
    {
        _logger.LogInformation("Iniciando procesamiento de cobro Wompi Sandbox con clave pública {Key}", _publicKey);

        // Control de Idempotencia
        if (!string.IsNullOrWhiteSpace(dto.ClaveIdempotencia))
        {
            var txExistente = await _context.TransaccionesPagos
                .FirstOrDefaultAsync(t => t.ClaveIdempotencia == dto.ClaveIdempotencia);

            if (txExistente != null)
            {
                return new ResultadoTransaccionDTO
                {
                    Exitoso = txExistente.Estado == "Aprobada",
                    Mensaje = $"Transacción previamente procesada por Wompi ({txExistente.Estado}). Código: {txExistente.CodigoAutorizacion}",
                    Transaccion = txExistente
                };
            }
        }

        var numeroLimpio = dto.NumeroTarjeta?.Replace(" ", "").Replace("-", "").Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(numeroLimpio) || numeroLimpio.Length < 13 || numeroLimpio.Length > 19 || !numeroLimpio.All(char.IsDigit))
        {
            return GenerarResultadoFallido(dto, "Wompi: Número de tarjeta inválido.");
        }

        if (!ValidarAlgoritmoLuhn(numeroLimpio))
        {
            return GenerarResultadoFallido(dto, "Wompi: Tarjeta no supera la validación Luhn.");
        }

        if (dto.Monto <= 0)
        {
            return GenerarResultadoFallido(dto, "Wompi: El monto debe ser mayor a cero.");
        }

        var marca = DetectarMarcaTarjeta(numeroLimpio);
        var ultimosDigitos = numeroLimpio.Substring(Math.Max(0, numeroLimpio.Length - 4));

        // En Wompi Sandbox, números que terminan en 9 simulan rechazo bancario
        bool aprobada = !numeroLimpio.EndsWith("9");
        string wompiTxId = $"WOMPI-SBX-{Guid.NewGuid().ToString("N")[..12].ToUpper()}";
        string mensaje = aprobada ? "Transacción aprobada por Wompi Sandbox." : "Transacción declinada por la entidad financiera (Wompi Sandbox).";

        var transaccion = new TransaccionPago
        {
            VentaId = dto.VentaId,
            Monto = Math.Round(dto.Monto, 2),
            Estado = aprobada ? "Aprobada" : "Rechazada",
            CodigoAutorizacion = aprobada ? wompiTxId : null,
            UltimosDigitosTarjeta = ultimosDigitos,
            MarcaTarjeta = marca,
            ClaveIdempotencia = dto.ClaveIdempotencia,
            MensajeRespuesta = mensaje,
            Fecha = DateTime.UtcNow
        };

        _context.TransaccionesPagos.Add(transaccion);
        await _context.SaveChangesAsync();

        if (!aprobada)
        {
            return new ResultadoTransaccionDTO
            {
                Exitoso = false,
                Mensaje = mensaje,
                Transaccion = transaccion
            };
        }

        PagoVenta? pagoRegistrado = null;
        if (dto.VentaId.HasValue && dto.VentaId.Value > 0)
        {
            pagoRegistrado = await _pagoVentaService.RegistrarAbonoAsync(
                dto.VentaId.Value,
                dto.Monto,
                $"Wompi - {marca} (****{ultimosDigitos})",
                dto.Responsable
            );
        }

        return new ResultadoTransaccionDTO
        {
            Exitoso = true,
            Mensaje = $"Pago aprobado por Wompi Sandbox. Referencia: {wompiTxId}",
            Transaccion = transaccion,
            PagoRegistrado = pagoRegistrado
        };
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
        return "Tarjeta";
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
