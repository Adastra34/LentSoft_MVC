using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class PasarelaPagoService : IPasarelaPagoService
{
    private readonly LentSoftDbContext _context;
    private readonly IPagoVentaService _pagoVentaService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PasarelaPagoService> _logger;
    private readonly ILogger _auditLogger;

    public const int MaxFailedAttemptsPerWindow = 5;
    private static readonly TimeSpan FailedAttemptsWindow = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan DuplicateChargeWindow = TimeSpan.FromSeconds(30);

    public PasarelaPagoService(
        LentSoftDbContext context,
        IPagoVentaService pagoVentaService,
        IMemoryCache? cache = null,
        ILogger<PasarelaPagoService>? logger = null,
        ILoggerFactory? loggerFactory = null)
    {
        _context = context;
        _pagoVentaService = pagoVentaService;
        _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
        _logger = logger ?? NullLogger<PasarelaPagoService>.Instance;
        _auditLogger = loggerFactory?.CreateLogger("Auditoria.Ventas") ?? (ILogger)_logger;
    }

    public async Task<ResultadoTransaccionDTO> ProcesarPagoTarjetaAsync(TarjetaPagoDTO dto)
    {
        var responsableKey = string.IsNullOrWhiteSpace(dto.Responsable)
            ? "vendedor_default"
            : dto.Responsable.Trim().ToLowerInvariant();
        var rateLimitCacheKey = $"RateLimitTarjeta_{responsableKey}";

        // 0. Límite de intentos fallidos de cobro con tarjeta (5 intentos en 10 minutos por usuario)
        if (_cache.TryGetValue<int>(rateLimitCacheKey, out var intentosFallidos) && intentosFallidos >= MaxFailedAttemptsPerWindow)
        {
            const string msgRateLimit = "Se ha superado el límite máximo de 5 intentos fallidos de cobro con tarjeta en 10 minutos. Por seguridad, espere unos minutos antes de intentar nuevamente.";
            _logger.LogWarning(
                "Bloqueo por límite de intentos fallidos de cobro con tarjeta ({Intentos}/{Max}) para Responsable={Responsable}, VentaId={VentaId}",
                intentosFallidos, MaxFailedAttemptsPerWindow, responsableKey, dto.VentaId);
            _auditLogger.LogWarning(
                "AUDITORIA_VENTAS | Operacion=BloqueoCobroTarjetaRateLimit | Entidad=Order#{VentaId} | IntentosFallidos={Intentos} | Responsable={Responsable} | FechaUtc={FechaUtc:O}",
                dto.VentaId ?? 0, intentosFallidos, responsableKey, DateTime.UtcNow);
            return GenerarResultadoFallido(dto, msgRateLimit);
        }

        // 1. Si viene con VentaId, validar ANTES de cobrar que la venta exista, esté activa y que el monto no supere el saldo
        if (dto.VentaId.HasValue && dto.VentaId.Value > 0)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Pagos)
                .FirstOrDefaultAsync(o => o.Id == dto.VentaId.Value);

            if (order == null || !order.Activo)
            {
                RegistrarIntentoFallido(rateLimitCacheKey);
                return GenerarResultadoFallido(dto, "No se encontró la venta especificada o se encuentra inactiva.");
            }

            if (order.Estado == "cancelado")
            {
                RegistrarIntentoFallido(rateLimitCacheKey);
                return GenerarResultadoFallido(dto, "No se pueden realizar cobros sobre una venta cancelada.");
            }

            var totalAbonado = order.Pagos != null && order.Pagos.Any()
                ? order.Pagos.Sum(p => p.Monto)
                : order.MontoPagado;
            var saldoActual = Math.Max(0m, order.Total - totalAbonado);

            if (dto.Monto <= 0)
            {
                RegistrarIntentoFallido(rateLimitCacheKey);
                return GenerarResultadoFallido(dto, "El monto a cobrar debe ser mayor a cero.");
            }

            if (dto.Monto > saldoActual)
            {
                RegistrarIntentoFallido(rateLimitCacheKey);
                return GenerarResultadoFallido(dto, $"El monto a cobrar (${dto.Monto:N2}) supera el saldo pendiente (${saldoActual:N2}).");
            }
        }
        else if (dto.Monto <= 0)
        {
            RegistrarIntentoFallido(rateLimitCacheKey);
            return GenerarResultadoFallido(dto, "El monto a cobrar debe ser mayor a cero.");
        }

        var numeroLimpio = dto.NumeroTarjeta?.Replace(" ", "").Replace("-", "").Trim() ?? string.Empty;

        // 2. Validaciones básicas de tarjeta (NUNCA se loguea ni persiste el número completo ni CVV)
        if (string.IsNullOrWhiteSpace(numeroLimpio) || numeroLimpio.Length < 13 || numeroLimpio.Length > 19 || !numeroLimpio.All(char.IsDigit))
        {
            RegistrarIntentoFallido(rateLimitCacheKey);
            return GenerarResultadoFallido(dto, "Número de tarjeta inválido. Debe contener entre 13 y 19 dígitos numéricos.");
        }

        if (!ValidarAlgoritmoLuhn(numeroLimpio))
        {
            RegistrarIntentoFallido(rateLimitCacheKey);
            return GenerarResultadoFallido(dto, "El número de tarjeta no supera la validación del algoritmo de Luhn.");
        }

        if (string.IsNullOrWhiteSpace(dto.NombreTitular) || dto.NombreTitular.Trim().Length < 3)
        {
            RegistrarIntentoFallido(rateLimitCacheKey);
            return GenerarResultadoFallido(dto, "Nombre del titular no válido.");
        }

        if (!ValidarFechaExpiracion(dto.FechaExpiracion))
        {
            RegistrarIntentoFallido(rateLimitCacheKey);
            return GenerarResultadoFallido(dto, "La tarjeta de crédito está vencida o el formato de fecha es incorrecto (MM/YY).");
        }

        if (string.IsNullOrWhiteSpace(dto.Cvv) || (dto.Cvv.Length != 3 && dto.Cvv.Length != 4) || !dto.Cvv.All(char.IsDigit))
        {
            RegistrarIntentoFallido(rateLimitCacheKey);
            return GenerarResultadoFallido(dto, "Código CVV inválido (debe tener 3 o 4 dígitos).");
        }

        var marca = DetectarMarcaTarjeta(numeroLimpio);
        var ultimosDigitos = numeroLimpio.Substring(Math.Max(0, numeroLimpio.Length - 4));
        var montoRedondeado = Math.Round(dto.Monto, 2);

        // 3. Rechazar cobros duplicados idénticos en ventana de 30 segundos
        var duplicateChargeKey = $"CobroTarjetaDup_{dto.VentaId ?? 0}_{montoRedondeado:0.00}_{ultimosDigitos}";
        if (_cache.TryGetValue(duplicateChargeKey, out _))
        {
            _logger.LogWarning(
                "Cobro con tarjeta duplicado bloqueado en ventana de 30s para VentaId={VentaId}, Monto={Monto}, Tarjeta=****{UltimosDigitos}",
                dto.VentaId, montoRedondeado, ultimosDigitos);
            return GenerarResultadoFallido(dto, "Ya se procesó un cobro con esta tarjeta por el mismo monto en los últimos 30 segundos. Evite el doble clic y verifique el estado del pedido.");
        }

        // 4. Simulación de aprobación/rechazo (Tarjeta terminada en '9' se rechaza por simulación bancaria)
        bool esAprobada = !numeroLimpio.EndsWith("9");
        string motivoRechazo = esAprobada ? string.Empty : "Transacción rechazada por el banco emisor (Fondos Insuficientes / Tarjeta de prueba rechazada).";
        var codigoAutorizacion = esAprobada ? $"AUTH-{Random.Shared.Next(100000, 999999)}" : null;

        var transaccion = new TransaccionPago
        {
            VentaId = dto.VentaId,
            Monto = montoRedondeado,
            Estado = esAprobada ? "Aprobada" : "Rechazada",
            CodigoAutorizacion = codigoAutorizacion,
            UltimosDigitosTarjeta = ultimosDigitos,
            MarcaTarjeta = marca,
            MensajeRespuesta = esAprobada ? "Transacción Aprobada Exitosamente" : motivoRechazo,
            Fecha = DateTime.UtcNow
        };

        // 5. Ejecución atómica en una sola transacción de base de datos con Rollback en caso de fallo
        bool ownsTx = _context.Database.CurrentTransaction == null;
        await using var dbTx = ownsTx ? await _context.Database.BeginTransactionAsync() : null;
        try
        {
            _context.TransaccionesPagos.Add(transaccion);
            await _context.SaveChangesAsync();

            if (!esAprobada)
            {
                if (dbTx != null) await dbTx.CommitAsync();
                RegistrarIntentoFallido(rateLimitCacheKey);

                _auditLogger.LogInformation(
                    "AUDITORIA_VENTAS | Operacion=CobroTarjetaRechazado | Entidad=TransaccionPago#{TxId}/Order#{VentaId} | Marca={Marca} | Tarjeta=****{UltimosDigitos} | Monto={Monto} | Estado=Rechazada | Responsable={Responsable} | FechaUtc={FechaUtc:O}",
                    transaccion.Id, dto.VentaId ?? 0, marca, ultimosDigitos, montoRedondeado, responsableKey, DateTime.UtcNow);

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
                    montoRedondeado,
                    $"Tarjeta {marca} (****{ultimosDigitos})",
                    dto.Responsable
                );
            }

            if (dbTx != null) await dbTx.CommitAsync();

            // Limpiar contador de intentos fallidos tras éxito y bloquear duplicado por 30s
            _cache.Remove(rateLimitCacheKey);
            _cache.Set(duplicateChargeKey, true, DuplicateChargeWindow);

            _auditLogger.LogInformation(
                "AUDITORIA_VENTAS | Operacion=CobroTarjetaAprobado | Entidad=TransaccionPago#{TxId}/Order#{VentaId} | CodigoAuth={CodigoAuth} | Marca={Marca} | Tarjeta=****{UltimosDigitos} | Monto={Monto} | Estado=Aprobada | Responsable={Responsable} | FechaUtc={FechaUtc:O}",
                transaccion.Id, dto.VentaId ?? 0, codigoAutorizacion, marca, ultimosDigitos, montoRedondeado, responsableKey, DateTime.UtcNow);

            return new ResultadoTransaccionDTO
            {
                Exitoso = true,
                Mensaje = $"Pago aprobado. Código de Autorización: {codigoAutorizacion}",
                Transaccion = transaccion,
                PagoRegistrado = pagoRegistrado
            };
        }
        catch (DbUpdateConcurrencyException concEx)
        {
            if (dbTx != null) await dbTx.RollbackAsync();
            _logger.LogWarning(concEx, "Conflicto de concurrencia durante el cobro con tarjeta para VentaId={VentaId}", dto.VentaId);
            throw new InvalidOperationException("El saldo de la venta fue modificado simultáneamente por otro usuario durante el cobro. Por favor, verifique el saldo e intente nuevamente.", concEx);
        }
        catch (Exception ex)
        {
            if (dbTx != null) await dbTx.RollbackAsync();
            _logger.LogError(ex, "Error en transacción de cobro con tarjeta para VentaId={VentaId}", dto.VentaId);
            throw;
        }
    }

    private void RegistrarIntentoFallido(string rateLimitCacheKey)
    {
        var actual = _cache.TryGetValue<int>(rateLimitCacheKey, out var count) ? count : 0;
        _cache.Set(rateLimitCacheKey, actual + 1, FailedAttemptsWindow);
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

/// <summary>
/// Implementación de pasarela de pago conectada al entorno Sandbox de Wompi Colombia (https://sandbox.wompi.co/v1).
///
/// CONFIGURACIÓN Y USO (Sección 4.3 — Interoperabilidad):
/// - Lee las credenciales exclusivamente desde variables de entorno (sin secretos en el repositorio):
///     * WOMPI_SANDBOX_PUBLIC_KEY  (ej. pub_test_...)
///     * WOMPI_SANDBOX_PRIVATE_KEY (ej. prv_test_...)
///     * WOMPI_SANDBOX_BASE_URL    (opcional, por defecto: https://sandbox.wompi.co/v1)
/// - Nunca almacena ni registra en logs el PAN completo ni el código CVV (solo últimos 4 dígitos y franquicia).
/// - Se mantiene desactivada por defecto para que el entorno local y las pruebas unitarias continúen
///   utilizando <see cref="PasarelaPagoService"/> sin requerir cambios en Program.cs.
/// </summary>
public class WompiSandboxPasarelaPagoService : IPasarelaPagoService
{
    private readonly LentSoftDbContext _context;
    private readonly IPagoVentaService _pagoVentaService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WompiSandboxPasarelaPagoService> _logger;
    private readonly PasarelaPagoService _localValidator;

    public WompiSandboxPasarelaPagoService(
        LentSoftDbContext context,
        IPagoVentaService pagoVentaService,
        HttpClient? httpClient = null,
        IMemoryCache? cache = null,
        ILogger<WompiSandboxPasarelaPagoService>? logger = null)
    {
        _context = context;
        _pagoVentaService = pagoVentaService;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _logger = logger ?? NullLogger<WompiSandboxPasarelaPagoService>.Instance;
        _localValidator = new PasarelaPagoService(context, pagoVentaService, cache);
    }

    public bool ValidarAlgoritmoLuhn(string numeroTarjeta) => _localValidator.ValidarAlgoritmoLuhn(numeroTarjeta);

    public string DetectarMarcaTarjeta(string numeroTarjeta) => _localValidator.DetectarMarcaTarjeta(numeroTarjeta);

    public async Task<ResultadoTransaccionDTO> ProcesarPagoTarjetaAsync(TarjetaPagoDTO dto)
    {
        var baseUrl = Environment.GetEnvironmentVariable("WOMPI_SANDBOX_BASE_URL") ?? "https://sandbox.wompi.co/v1";
        var publicKey = Environment.GetEnvironmentVariable("WOMPI_SANDBOX_PUBLIC_KEY");
        var privateKey = Environment.GetEnvironmentVariable("WOMPI_SANDBOX_PRIVATE_KEY");

        // Si las credenciales sandbox no están configuradas en el entorno, delegar de forma segura al validador local
        if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey))
        {
            _logger.LogInformation("Variables WOMPI_SANDBOX_* no definidas en el entorno; ejecutando simulación sandbox local.");
            return await _localValidator.ProcesarPagoTarjetaAsync(dto);
        }

        var numeroLimpio = dto.NumeroTarjeta?.Replace(" ", "").Replace("-", "").Trim() ?? string.Empty;
        if (!ValidarAlgoritmoLuhn(numeroLimpio))
        {
            return new ResultadoTransaccionDTO
            {
                Exitoso = false,
                Mensaje = "El número de tarjeta no supera la validación Luhn para Wompi Sandbox."
            };
        }

        var parts = (dto.FechaExpiracion ?? "").Split('/');
        var expMonth = parts.Length == 2 ? parts[0].Trim() : "12";
        var expYear = parts.Length == 2 ? parts[1].Trim() : "29";
        var marca = DetectarMarcaTarjeta(numeroLimpio);
        var ultimosDigitos = numeroLimpio.Substring(Math.Max(0, numeroLimpio.Length - 4));
        var montoCentavos = (long)(Math.Round(dto.Monto, 2) * 100);

        try
        {
            // 1. Tokenizar tarjeta en Wompi Sandbox (/tokens/cards) con Public Key
            using var tokenReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/tokens/cards");
            tokenReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", publicKey);
            var tokenPayload = JsonSerializer.Serialize(new
            {
                number = numeroLimpio,
                cvc = dto.Cvv,
                exp_month = expMonth,
                exp_year = expYear,
                card_holder = dto.NombreTitular
            });
            tokenReq.Content = new StringContent(tokenPayload, Encoding.UTF8, "application/json");

            using var tokenRes = await _httpClient.SendAsync(tokenReq);
            if (!tokenRes.IsSuccessStatusCode)
            {
                return new ResultadoTransaccionDTO
                {
                    Exitoso = false,
                    Mensaje = $"Wompi Sandbox rechazó la tokenización de la tarjeta (HTTP {(int)tokenRes.StatusCode})."
                };
            }

            using var tokenDoc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
            var cardToken = tokenDoc.RootElement.GetProperty("data").GetProperty("id").GetString();

            // 2. Crear cargo en Wompi Sandbox (/transactions) con Private Key
            using var txReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/transactions");
            txReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", privateKey);
            var reference = $"LENTSOFT-ORD-{dto.VentaId ?? 0}-{DateTime.UtcNow.Ticks}";
            var txPayload = JsonSerializer.Serialize(new
            {
                amount_in_cents = montoCentavos,
                currency = "COP",
                customer_email = "clientes@lentsoft.com",
                reference,
                payment_method = new
                {
                    type = "CARD",
                    token = cardToken,
                    installments = 1
                }
            });
            txReq.Content = new StringContent(txPayload, Encoding.UTF8, "application/json");

            using var txRes = await _httpClient.SendAsync(txReq);
            using var txDoc = await JsonDocument.ParseAsync(await txRes.Content.ReadAsStreamAsync());
            var status = txDoc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.TryGetProperty("status", out var stEl)
                ? stEl.GetString() ?? "ERROR"
                : "ERROR";
            var wompiTxId = dataEl.ValueKind == JsonValueKind.Object && dataEl.TryGetProperty("id", out var idEl)
                ? idEl.GetString()
                : $"WOMPI-{Random.Shared.Next(100000, 999999)}";

            bool aprobada = string.Equals(status, "APPROVED", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(status, "PENDING", StringComparison.OrdinalIgnoreCase);

            bool ownsTx = _context.Database.CurrentTransaction == null;
            await using var dbTx = ownsTx ? await _context.Database.BeginTransactionAsync() : null;

            var transaccion = new TransaccionPago
            {
                VentaId = dto.VentaId,
                Monto = Math.Round(dto.Monto, 2),
                Estado = aprobada ? "Aprobada" : "Rechazada",
                CodigoAutorizacion = aprobada ? wompiTxId : null,
                UltimosDigitosTarjeta = ultimosDigitos,
                MarcaTarjeta = marca,
                MensajeRespuesta = $"Wompi Sandbox: {status}",
                Fecha = DateTime.UtcNow
            };

            _context.TransaccionesPagos.Add(transaccion);
            await _context.SaveChangesAsync();

            PagoVenta? pagoRegistrado = null;
            if (aprobada && dto.VentaId.HasValue && dto.VentaId.Value > 0)
            {
                pagoRegistrado = await _pagoVentaService.RegistrarAbonoAsync(
                    dto.VentaId.Value,
                    Math.Round(dto.Monto, 2),
                    $"Wompi {marca} (****{ultimosDigitos})",
                    dto.Responsable);
            }

            if (dbTx != null) await dbTx.CommitAsync();

            return new ResultadoTransaccionDTO
            {
                Exitoso = aprobada,
                Mensaje = aprobada ? $"Pago Wompi Sandbox aprobado ({wompiTxId})." : $"Pago rechazado por Wompi Sandbox ({status}).",
                Transaccion = transaccion,
                PagoRegistrado = pagoRegistrado
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error de comunicación con Wompi Sandbox para VentaId={VentaId}", dto.VentaId);
            return new ResultadoTransaccionDTO
            {
                Exitoso = false,
                Mensaje = "No fue posible completar la comunicación con la pasarela Wompi Sandbox. Intente nuevamente."
            };
        }
    }
}
