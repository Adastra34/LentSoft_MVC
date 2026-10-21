using Microsoft.Extensions.Caching.Memory;

namespace LentSoft.Web.Services;

/// <summary>
/// Implementación en memoria (singleton sobre IMemoryCache) del servicio de control de intentos de inicio de sesión.
/// Aplica la regla de seguridad: 5 intentos fallidos en una ventana de 15 minutos causan un bloqueo temporal de 15 minutos.
/// La clave de seguimiento combina el correo en minúsculas y la dirección IP del cliente.
/// </summary>
public class LoginAttemptService : ILoginAttemptService
{
    private readonly IMemoryCache _cache;
    private readonly int _maxAttempts;
    private readonly TimeSpan _window;
    private readonly TimeSpan _lockoutDuration;
    private readonly Func<DateTime> _utcNow;
    private readonly object _syncRoot = new();

    /// <summary>
    /// Constructor principal utilizado por el contenedor de inyección de dependencias.
    /// Configura los valores predeterminados: 5 intentos máximos, ventana de 15 minutos y bloqueo de 15 minutos.
    /// </summary>
    /// <param name="cache">Caché en memoria.</param>
    public LoginAttemptService(IMemoryCache cache)
        : this(cache, 5, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15), null)
    {
    }

    /// <summary>
    /// Constructor parametrizable para pruebas unitarias que permite ajustar ventanas de tiempo o inyectar un proveedor de hora UTC.
    /// </summary>
    public LoginAttemptService(
        IMemoryCache cache,
        int maxAttempts,
        TimeSpan window,
        TimeSpan lockoutDuration,
        Func<DateTime>? utcNowProvider = null)
    {
        _cache = cache;
        _maxAttempts = maxAttempts > 0 ? maxAttempts : 5;
        _window = window;
        _lockoutDuration = lockoutDuration;
        _utcNow = utcNowProvider ?? (() => DateTime.UtcNow);
    }

    private string GetCacheKey(string key) => $"login_attempt:{key}";

    /// <inheritdoc />
    public bool IsLockedOut(string key, out TimeSpan remaining)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            remaining = TimeSpan.Zero;
            return false;
        }

        lock (_syncRoot)
        {
            var cacheKey = GetCacheKey(key);
            if (_cache.TryGetValue<AttemptState>(cacheKey, out var state) && state != null)
            {
                var now = _utcNow();
                if (state.LockoutEndUtc.HasValue)
                {
                    if (state.LockoutEndUtc.Value > now)
                    {
                        remaining = state.LockoutEndUtc.Value - now;
                        return true;
                    }
                    else
                    {
                        // El período de bloqueo ha expirado: limpiar estado
                        _cache.Remove(cacheKey);
                    }
                }
            }

            remaining = TimeSpan.Zero;
            return false;
        }
    }

    /// <inheritdoc />
    public void RegisterFailure(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        lock (_syncRoot)
        {
            var cacheKey = GetCacheKey(key);
            var now = _utcNow();

            if (!_cache.TryGetValue<AttemptState>(cacheKey, out var state) || state == null)
            {
                state = new AttemptState();
            }

            // Si ya estaba en bloqueo vigente, no registrar más intentos
            if (state.LockoutEndUtc.HasValue && state.LockoutEndUtc.Value > now)
            {
                return;
            }

            // Descartar intentos fallidos fuera de la ventana móvil
            state.FailureTimestamps.RemoveAll(t => t < now - _window);

            // Registrar nuevo intento fallido
            state.FailureTimestamps.Add(now);

            // Evaluar si se alcanzó el umbral de bloqueo
            if (state.FailureTimestamps.Count >= _maxAttempts)
            {
                state.LockoutEndUtc = now.Add(_lockoutDuration);
                state.FailureTimestamps.Clear();
            }

            // Guardar en caché con expiración absoluta igual al tiempo máximo necesario
            var cacheExpiration = _lockoutDuration > _window ? _lockoutDuration : _window;
            _cache.Set(cacheKey, state, cacheExpiration.Add(TimeSpan.FromMinutes(5)));
        }
    }

    /// <inheritdoc />
    public void Reset(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        lock (_syncRoot)
        {
            _cache.Remove(GetCacheKey(key));
        }
    }

    private class AttemptState
    {
        public List<DateTime> FailureTimestamps { get; } = new();
        public DateTime? LockoutEndUtc { get; set; }
    }
}
