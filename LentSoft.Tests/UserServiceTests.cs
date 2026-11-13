using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class UserServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();

        _userService = new UserService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ChangePasswordAsync_ConPasswordActualCorrectaEIncorrecta()
    {
        // Arrange
        var passwordActual = "ClaveActual123*";
        var passwordNueva = "ClaveNueva456*";

        var user = new User
        {
            Nombre = "Roberto",
            Apellido = "Sánchez",
            TipoDocumento = "CC",
            NumeroDocumento = "88112233",
            Email = "roberto.sanchez@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordActual),
            Role = "usuario",
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act 1: Intento con contraseña actual incorrecta
        var fallo = await _userService.ChangePasswordAsync(user.Id, "ClaveErronea999*", passwordNueva);
        Assert.False(fallo);

        // Act 2: Intento con contraseña actual correcta
        var exito = await _userService.ChangePasswordAsync(user.Id, passwordActual, passwordNueva);
        Assert.True(exito);

        // Assert: La base de datos contiene el nuevo hash
        var userDb = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(userDb);
        Assert.True(BCrypt.Net.BCrypt.Verify(passwordNueva, userDb.PasswordHash));
    }

    [Fact]
    public async Task DeleteAsync_Y_ReactivateAsync_ModificanEstadoActivo()
    {
        // Arrange
        var user = new User
        {
            Nombre = "Mariana",
            Apellido = "Díaz",
            TipoDocumento = "CC",
            NumeroDocumento = "88112234",
            Email = "mariana.diaz@test.com",
            PasswordHash = "hash123",
            Role = "usuario",
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act 1: Desactivar
        var deleteOk = await _userService.DeleteAsync(user.Id);
        Assert.True(deleteOk);

        var userDesactivado = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(userDesactivado);
        Assert.False(userDesactivado.Activo);

        // Act 2: Reactivar
        var reactivateOk = await _userService.ReactivateAsync(user.Id);
        Assert.True(reactivateOk);

        var userReactivado = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(userReactivado);
        Assert.True(userReactivado.Activo);
    }

    [Fact]
    public async Task DeactivateAsync_NoDesactivaAlUnicoAdminActivo()
    {
        // Arrange: El DbContext sembrado tiene 1 admin activo (Id 1: admin@lentsoft.com)
        var seededAdmin = await _context.Users.FindAsync(1);
        Assert.NotNull(seededAdmin);
        Assert.True(seededAdmin.Activo);
        Assert.Equal("admin", seededAdmin.Role.ToLower());

        // Act: Un usuario diferente intenta desactivar al único admin
        var (success, errorMessage) = await _userService.DeactivateAsync(1, currentUserId: 999);

        // Assert: Se rechaza por RF-M07-03
        Assert.False(success);
        Assert.Equal("No se puede eliminar el único usuario administrador restante del sistema.", errorMessage);

        // Verificar que sigue activo
        var adminDb = await _context.Users.FindAsync(1);
        Assert.NotNull(adminDb);
        Assert.True(adminDb.Activo);
    }

    [Fact]
    public async Task DeactivateAsync_NoAutoDesactivarse()
    {
        // Arrange: El admin 1 intenta desactivarse a sí mismo
        var currentAdminId = 1;

        // Act
        var (success, errorMessage) = await _userService.DeactivateAsync(currentAdminId, currentUserId: currentAdminId);

        // Assert: Rechazado
        Assert.False(success);
        Assert.Equal("No puedes desactivar tu propia cuenta.", errorMessage);

        // Verificar que sigue activo
        var adminDb = await _context.Users.FindAsync(currentAdminId);
        Assert.NotNull(adminDb);
        Assert.True(adminDb.Activo);
    }

    [Fact]
    public async Task DeactivateAsync_ReglaAdminIgnoraAdminsInactivos()
    {
        // Arrange:
        // Tenemos el admin 1 activo sembrado.
        // Agregamos un admin 2 pero inactivo.
        var inactiveAdmin = new User
        {
            Nombre = "Admin",
            Apellido = "Inactivo",
            TipoDocumento = "CC",
            NumeroDocumento = "9900112233",
            Email = "admin.inactivo@test.com",
            PasswordHash = "hash",
            Role = "admin",
            Activo = false,
            FechaRegistro = DateTime.UtcNow
        };
        _context.Users.Add(inactiveAdmin);
        await _context.SaveChangesAsync();

        // En la base de datos hay 2 admins en total, pero solo 1 activo.
        // Si intentamos desactivar al admin 1, la regla debe ignorar al inactivo y fallar.
        var (failSuccess, failError) = await _userService.DeactivateAsync(1, currentUserId: 999);
        Assert.False(failSuccess);
        Assert.Equal("No se puede eliminar el único usuario administrador restante del sistema.", failError);

        // Ahora creamos un admin 3 activo. Con 2 admins activos en el sistema,
        // desactivar al admin 3 desde la sesión del admin 1 debe ser permitido.
        var activeAdmin2 = new User
        {
            Nombre = "Segundo",
            Apellido = "Administrador",
            TipoDocumento = "CC",
            NumeroDocumento = "9900112244",
            Email = "admin2@test.com",
            PasswordHash = "hash",
            Role = "admin",
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };
        _context.Users.Add(activeAdmin2);
        await _context.SaveChangesAsync();

        var (success, error) = await _userService.DeactivateAsync(activeAdmin2.Id, currentUserId: 1);
        Assert.True(success);
        Assert.Null(error);

        var admin2Db = await _context.Users.FindAsync(activeAdmin2.Id);
        Assert.NotNull(admin2Db);
        Assert.False(admin2Db.Activo);
    }

    [Theory]
    [InlineData("superadmin")]
    [InlineData("invitado")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ChangeRoleAsync_RolInvalido_RetornaError(string invalidRole)
    {
        // Arrange
        var user = new User
        {
            Nombre = "Carlos",
            Apellido = "Gómez",
            TipoDocumento = "CC",
            NumeroDocumento = "11223344",
            Email = "carlos.gomez@test.com",
            PasswordHash = "hash",
            Role = "usuario",
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var (success, errorMessage) = await _userService.ChangeRoleAsync(user.Id, invalidRole, currentUserId: 1);

        // Assert
        Assert.False(success);
        Assert.Equal("El rol especificado no es válido.", errorMessage);
    }

    [Fact]
    public async Task ChangeRoleAsync_UsuarioInexistente_RetornaError()
    {
        // Act
        var (success, errorMessage) = await _userService.ChangeRoleAsync(99999, "ventas", currentUserId: 1);

        // Assert
        Assert.False(success);
        Assert.Equal("Usuario no encontrado.", errorMessage);
    }

    [Fact]
    public async Task ChangeRoleAsync_CambioValido_UsuarioAVentas_ActualizaRol()
    {
        // Arrange
        var user = new User
        {
            Nombre = "Laura",
            Apellido = "Pérez",
            TipoDocumento = "CC",
            NumeroDocumento = "12345678",
            Email = "laura.perez@test.com",
            PasswordHash = "hash",
            Role = "usuario",
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var (success, errorMessage) = await _userService.ChangeRoleAsync(user.Id, "ventas", currentUserId: 1);

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);

        var updated = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(updated);
        Assert.Equal("ventas", updated.Role);
    }

    [Fact]
    public async Task ChangeRoleAsync_UnicoAdminActivo_NoPermiteDegradar()
    {
        // Arrange: El admin 1 es el único admin activo sembrado
        var seededAdmin = await _context.Users.FindAsync(1);
        Assert.NotNull(seededAdmin);
        Assert.Equal("admin", seededAdmin.Role.ToLower());
        Assert.True(seededAdmin.Activo);

        // Act: Otro usuario intenta degradar al único admin activo a usuario
        var (success, errorMessage) = await _userService.ChangeRoleAsync(1, "usuario", currentUserId: 999);

        // Assert
        Assert.False(success);
        Assert.Equal("No se puede cambiar el rol del único administrador activo del sistema.", errorMessage);

        var adminDb = await _context.Users.FindAsync(1);
        Assert.NotNull(adminDb);
        Assert.Equal("admin", adminDb.Role);
    }

    [Fact]
    public async Task ChangeRoleAsync_HabiendoOtroAdminActivo_PermiteDegradarAdmin()
    {
        // Arrange: Creamos un segundo admin activo
        var secondAdmin = new User
        {
            Nombre = "Segundo",
            Apellido = "Admin",
            TipoDocumento = "CC",
            NumeroDocumento = "55667788",
            Email = "segundo.admin@test.com",
            PasswordHash = "hash",
            Role = "admin",
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };
        _context.Users.Add(secondAdmin);
        await _context.SaveChangesAsync();

        // Act: El admin 1 degrada al admin 2 a ventas
        var (success, errorMessage) = await _userService.ChangeRoleAsync(secondAdmin.Id, "ventas", currentUserId: 1);

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);

        var updated = await _context.Users.FindAsync(secondAdmin.Id);
        Assert.NotNull(updated);
        Assert.Equal("ventas", updated.Role);
    }

    [Fact]
    public async Task ChangeRoleAsync_AdminNoPuedeDegradarseASiMismo_RetornaError()
    {
        // Arrange: El admin 1 intenta cambiarse a sí mismo de admin a optometra
        var currentAdminId = 1;

        // Act
        var (success, errorMessage) = await _userService.ChangeRoleAsync(currentAdminId, "optometra", currentUserId: currentAdminId);

        // Assert
        Assert.False(success);
        Assert.Equal("No puedes cambiar tu propio rol de administrador.", errorMessage);

        var adminDb = await _context.Users.FindAsync(currentAdminId);
        Assert.NotNull(adminDb);
        Assert.Equal("admin", adminDb.Role);
    }

    [Fact]
    public async Task ChangeRoleAsync_NormalizaRolAMinusculas()
    {
        // Arrange
        var user = new User
        {
            Nombre = "Andrés",
            Apellido = "Molina",
            TipoDocumento = "CC",
            NumeroDocumento = "98765432",
            Email = "andres.molina@test.com",
            PasswordHash = "hash",
            Role = "usuario",
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act 1: Pasar "VENTAS"
        var (success1, _) = await _userService.ChangeRoleAsync(user.Id, "VENTAS", currentUserId: 1);
        Assert.True(success1);

        var userDb1 = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(userDb1);
        Assert.Equal("ventas", userDb1.Role);

        // Act 2: Pasar "Admin"
        var (success2, _) = await _userService.ChangeRoleAsync(user.Id, "Admin", currentUserId: 1);
        Assert.True(success2);

        var userDb2 = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(userDb2);
        Assert.Equal("admin", userDb2.Role);
    }
}
