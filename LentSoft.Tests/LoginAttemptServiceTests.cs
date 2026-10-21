using System;
using Microsoft.Extensions.Caching.Memory;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class LoginAttemptServiceTests : IDisposable
{
    private readonly IMemoryCache _cache;
    private DateTime _simulatedNow;

    public LoginAttemptServiceTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _simulatedNow = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    private LoginAttemptService CreateService(TimeSpan? window = null, TimeSpan? lockoutDuration = null, int maxAttempts = 5)
    {
        return new LoginAttemptService(
            _cache,
            maxAttempts: maxAttempts,
            window: window ?? TimeSpan.FromMinutes(15),
            lockoutDuration: lockoutDuration ?? TimeSpan.FromMinutes(15),
            utcNowProvider: () => _simulatedNow
        );
    }

    [Fact]
    public void RegisterFailure_BloqueaAlQuintoFallo()
    {
        // Arrange
        var service = CreateService();
        var key = "victima@test.com:192.168.1.50";

        // Act: 4 fallos iniciales
        for (int i = 0; i < 4; i++)
        {
            service.RegisterFailure(key);
            Assert.False(service.IsLockedOut(key, out _));
        }

        // 5.º fallo activa el bloqueo
        service.RegisterFailure(key);

        // Assert
        var isLocked = service.IsLockedOut(key, out var remaining);
        Assert.True(isLocked);
        Assert.True(remaining.TotalMinutes > 14 && remaining.TotalMinutes <= 15);
    }

    [Fact]
    public void IsLockedOut_ExpiraTrasLaVentanaDeBloqueo()
    {
        // Arrange
        var service = CreateService();
        var key = "usuario.expira@test.com:10.0.0.1";

        for (int i = 0; i < 5; i++)
        {
            service.RegisterFailure(key);
        }

        Assert.True(service.IsLockedOut(key, out _));

        // Act: Avanzar el reloj 16 minutos (el bloqueo configurado es de 15 minutos)
        _simulatedNow = _simulatedNow.AddMinutes(16);

        // Assert: El bloqueo ya debe haber expirado
        var isLocked = service.IsLockedOut(key, out var remaining);
        Assert.False(isLocked);
        Assert.Equal(TimeSpan.Zero, remaining);
    }

    [Fact]
    public void Reset_LimpiaElContadorYDesbloquea()
    {
        // Arrange
        var service = CreateService();
        var key = "usuario.reset@test.com:172.16.0.1";

        for (int i = 0; i < 5; i++)
        {
            service.RegisterFailure(key);
        }

        Assert.True(service.IsLockedOut(key, out _));

        // Act
        service.Reset(key);

        // Assert
        Assert.False(service.IsLockedOut(key, out _));
    }

    [Fact]
    public void RegisterFailure_ClavesDistintasNoSeAfectan()
    {
        // Arrange
        var service = CreateService();
        var key1 = "cuenta@test.com:192.168.1.1";
        var key2 = "cuenta@test.com:192.168.1.2"; // Misma cuenta, diferente IP
        var key3 = "otra@test.com:192.168.1.1";   // Misma IP, diferente cuenta

        for (int i = 0; i < 5; i++)
        {
            service.RegisterFailure(key1);
        }

        // Assert: key1 bloqueado, pero ni key2 ni key3 se ven afectados
        Assert.True(service.IsLockedOut(key1, out _));
        Assert.False(service.IsLockedOut(key2, out _));
        Assert.False(service.IsLockedOut(key3, out _));
    }
}
