using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using LentSoft.Web.Controllers;
using LentSoft.Web.Controllers.Api;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class AppointmentTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;

    public AppointmentTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private class FakeTempDataProvider : ITempDataProvider
    {
        public System.Collections.Generic.IDictionary<string, object> LoadTempData(HttpContext context) => new System.Collections.Generic.Dictionary<string, object>();
        public void SaveTempData(HttpContext context, System.Collections.Generic.IDictionary<string, object> values) { }
    }

    [Theory]
    [InlineData(8, 0, true)]   // 08:00 AM Lunes -> Válido
    [InlineData(17, 0, true)]  // 05:00 PM Lunes -> Válido (termina 18:00)
    [InlineData(17, 30, false)] // 05:30 PM Lunes -> Rechazado (termina 18:30)
    [InlineData(18, 0, false)]  // 06:00 PM Lunes -> Rechazado (termina 19:00)
    [InlineData(7, 59, false)]  // 07:59 AM Lunes -> Rechazado
    public void EsHorarioLaboral_ValidaInicioYFinConDuracion(int hora, int minuto, bool esperado)
    {
        // Lunes 26 de Octubre de 2026 (Día laboral)
        var fechaLunes = new DateTime(2026, 10, 26, hora, minuto, 0);
        var resultado = Appointment.EsHorarioLaboral(fechaLunes);
        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void EsHorarioLaboral_RechazaDomingosEnCualquierHorario()
    {
        // Domingo 25 de Octubre de 2026
        var domingoManana = new DateTime(2026, 10, 25, 10, 0, 0);
        var domingoTarde = new DateTime(2026, 10, 25, 15, 0, 0);

        Assert.False(Appointment.EsHorarioLaboral(domingoManana));
        Assert.False(Appointment.EsHorarioLaboral(domingoTarde));
    }

    [Fact]
    public async Task Optometra_UpdateAppointmentStatus_RechazaEstadoInvalido()
    {
        // Arrange
        var controller = new OptometraController(_context);
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider());

        var cita = new Appointment
        {
            UserId = 2,
            OptometraId = 3,
            Servicio = "Examen Visual",
            FechaHora = new DateTime(2026, 10, 26, 10, 0, 0),
            Estado = "pendiente",
            Activo = true
        };
        _context.Appointments.Add(cita);
        await _context.SaveChangesAsync();

        // Act - Enviar estado inventado "xyz"
        var result = await controller.UpdateAppointmentStatus(cita.Id, "xyz") as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("citas", result.RouteValues?["section"]);
        Assert.Contains("Estado no válido", controller.TempData["ErrorMessage"]?.ToString());

        var citaEnDb = await _context.Appointments.FindAsync(cita.Id);
        Assert.NotNull(citaEnDb);
        Assert.Equal("pendiente", citaEnDb.Estado); // No cambió
    }

    [Fact]
    public async Task Optometra_UpdateAppointmentStatus_AceptaEstadoValidoYNormalizaMinusculas()
    {
        // Arrange
        var controller = new OptometraController(_context);
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider());

        var cita = new Appointment
        {
            UserId = 2,
            OptometraId = 3,
            Servicio = "Examen Visual",
            FechaHora = new DateTime(2026, 10, 26, 10, 0, 0),
            Estado = "pendiente",
            Activo = true
        };
        _context.Appointments.Add(cita);
        await _context.SaveChangesAsync();

        // Act - Enviar "CONFIRMADA" con mayúsculas
        var result = await controller.UpdateAppointmentStatus(cita.Id, "CONFIRMADA") as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("citas", result.RouteValues?["section"]);
        Assert.Equal("Estado de cita actualizado.", controller.TempData["SuccessMessage"]?.ToString());

        var citaEnDb = await _context.Appointments.FindAsync(cita.Id);
        Assert.NotNull(citaEnDb);
        Assert.Equal("confirmada", citaEnDb.Estado); // Normalizado a minúsculas
    }

    [Fact]
    public async Task Optometra_UpdateAppointmentStatus_CitaExistente1730_PermiteCambioDeEstado()
    {
        // Arrange: Cita existente previamente a las 17:30
        var controller = new OptometraController(_context);
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider());

        var citaPrevia1730 = new Appointment
        {
            UserId = 2,
            OptometraId = 3,
            Servicio = "Examen Visual",
            FechaHora = new DateTime(2026, 10, 26, 17, 30, 0),
            Estado = "pendiente",
            Activo = true
        };
        _context.Appointments.Add(citaPrevia1730);
        await _context.SaveChangesAsync();

        // Act: Cambiar estado a "completada"
        var result = await controller.UpdateAppointmentStatus(citaPrevia1730.Id, "completada") as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        var citaEnDb = await _context.Appointments.FindAsync(citaPrevia1730.Id);
        Assert.NotNull(citaEnDb);
        Assert.Equal("completada", citaEnDb.Estado);
    }

    [Fact]
    public async Task AppointmentsApi_Rechaza1730_1800_YDomingo()
    {
        // Arrange
        var api = new AppointmentsApiController(_context);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "2") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        api.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        // 1. Probar 17:30 (Lunes futuro en hora local)
        var request1730 = new CreateAppointmentApiRequest
        {
            Servicio = "Examen Visual",
            FechaHora = new DateTime(2026, 10, 26, 17, 30, 0)
        };
        var result1730 = await api.CreateAppointment(request1730) as BadRequestObjectResult;
        Assert.NotNull(result1730);

        // 2. Probar 18:00 (Lunes futuro en hora local)
        var request1800 = new CreateAppointmentApiRequest
        {
            Servicio = "Examen Visual",
            FechaHora = new DateTime(2026, 10, 26, 18, 0, 0)
        };
        var result1800 = await api.CreateAppointment(request1800) as BadRequestObjectResult;
        Assert.NotNull(result1800);

        // 3. Probar Domingo (2026-10-25)
        var requestDomingo = new CreateAppointmentApiRequest
        {
            Servicio = "Examen Visual",
            FechaHora = new DateTime(2026, 10, 25, 10, 0, 0)
        };
        var resultDomingo = await api.CreateAppointment(requestDomingo) as BadRequestObjectResult;
        Assert.NotNull(resultDomingo);
    }

    private DashboardController CrearDashboardController(int userId = 1, string role = "admin")
    {
        var controller = new DashboardController(
            null!,
            null!,
            new UserService(_context),
            new OrderService(_context),
            new FavoriteService(_context),
            null!,
            null!,
            NullLogger<DashboardController>.Instance,
            _context,
            Options.Create(new InventorySettings()));

        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider());
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
        return controller;
    }

    private OptometraController CrearOptometraController(int userId = 3)
    {
        var controller = new OptometraController(_context, logger: NullLogger<OptometraController>.Instance);
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider());
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "optometra")
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
        return controller;
    }

    // ==========================================
    // REQUERIMIENTO 1: UpdateAppointmentStatus
    // ==========================================

    [Fact]
    public async Task Optometra_UpdateAppointmentStatus_CitaInactiva_Rechaza()
    {
        var controller = CrearOptometraController();
        var cita = new Appointment
        {
            UserId = 2,
            OptometraId = 3,
            Servicio = "Examen",
            FechaHora = DateTime.UtcNow.AddDays(1),
            Estado = "pendiente",
            Activo = false
        };
        _context.Appointments.Add(cita);
        await _context.SaveChangesAsync();

        var result = await controller.UpdateAppointmentStatus(cita.Id, "confirmada") as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Cita no encontrada.", controller.TempData["ErrorMessage"]?.ToString());
    }

    [Fact]
    public async Task Optometra_UpdateAppointmentStatus_CitaCompletada_NoPermiteCambiarAOtroEstado()
    {
        var controller = CrearOptometraController();
        var cita = new Appointment
        {
            UserId = 2,
            OptometraId = 3,
            Servicio = "Examen",
            FechaHora = DateTime.UtcNow.AddDays(1),
            Estado = "completada",
            Activo = true
        };
        _context.Appointments.Add(cita);
        await _context.SaveChangesAsync();

        var result = await controller.UpdateAppointmentStatus(cita.Id, "pendiente") as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("No se puede cambiar el estado de una cita completada.", controller.TempData["ErrorMessage"]?.ToString());
        var citaEnDb = await _context.Appointments.FindAsync(cita.Id);
        Assert.Equal("completada", citaEnDb!.Estado);
    }

    [Fact]
    public async Task Optometra_UpdateAppointmentStatus_CitaCompletada_PermiteMismoEstado()
    {
        var controller = CrearOptometraController();
        var cita = new Appointment
        {
            UserId = 2,
            OptometraId = 3,
            Servicio = "Examen",
            FechaHora = DateTime.UtcNow.AddDays(1),
            Estado = "completada",
            Activo = true
        };
        _context.Appointments.Add(cita);
        await _context.SaveChangesAsync();

        var result = await controller.UpdateAppointmentStatus(cita.Id, "completada") as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Estado de cita actualizado.", controller.TempData["SuccessMessage"]?.ToString());
    }

    [Fact]
    public async Task Optometra_UpdateAppointmentStatus_CitaCancelada_ReactivarConCruce_Rechaza()
    {
        var controller = CrearOptometraController(3);
        var fecha = new DateTime(2026, 11, 23, 10, 0, 0); // Lunes laboral

        var citaActiva = new Appointment
        {
            UserId = 2,
            OptometraId = 3,
            Servicio = "Examen",
            FechaHora = fecha,
            Estado = "confirmada",
            Activo = true
        };
        var citaCancelada = new Appointment
        {
            UserId = 4,
            OptometraId = 3,
            Servicio = "Examen",
            FechaHora = fecha,
            Estado = "cancelada",
            Activo = true
        };
        _context.Appointments.AddRange(citaActiva, citaCancelada);
        await _context.SaveChangesAsync();

        var result = await controller.UpdateAppointmentStatus(citaCancelada.Id, "confirmada") as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("No es posible reactivar la cita: el horario ya está ocupado por otra cita.", controller.TempData["ErrorMessage"]?.ToString());
        var citaDb = await _context.Appointments.FindAsync(citaCancelada.Id);
        Assert.Equal("cancelada", citaDb!.Estado);
    }

    [Fact]
    public async Task Optometra_UpdateAppointmentStatus_CitaCancelada_ReactivarSinCruce_Actualiza()
    {
        var controller = CrearOptometraController(3);
        var fecha = new DateTime(2026, 11, 23, 10, 0, 0);

        var citaCancelada = new Appointment
        {
            UserId = 4,
            OptometraId = 3,
            Servicio = "Examen",
            FechaHora = fecha,
            Estado = "cancelada",
            Activo = true
        };
        _context.Appointments.Add(citaCancelada);
        await _context.SaveChangesAsync();

        var result = await controller.UpdateAppointmentStatus(citaCancelada.Id, "confirmada") as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Estado de cita actualizado.", controller.TempData["SuccessMessage"]?.ToString());
        var citaDb = await _context.Appointments.FindAsync(citaCancelada.Id);
        Assert.Equal("confirmada", citaDb!.Estado);
    }

    [Fact]
    public async Task DashboardAdmin_UpdateAppointmentStatus_CitaInactiva_Y_Completada()
    {
        var controller = CrearDashboardController();
        var citaInactiva = new Appointment
        {
            UserId = 2,
            OptometraId = 3,
            Servicio = "Examen",
            FechaHora = DateTime.UtcNow.AddDays(1),
            Estado = "pendiente",
            Activo = false
        };
        var citaCompletada = new Appointment
        {
            UserId = 2,
            OptometraId = 3,
            Servicio = "Examen",
            FechaHora = DateTime.UtcNow.AddDays(2),
            Estado = "completada",
            Activo = true
        };
        _context.Appointments.AddRange(citaInactiva, citaCompletada);
        await _context.SaveChangesAsync();

        // 1. Inactiva -> rechaza
        var resInactiva = await controller.UpdateAppointmentStatus(citaInactiva.Id, "confirmada") as RedirectToActionResult;
        Assert.Equal("Cita no encontrada.", controller.TempData["ErrorMessage"]?.ToString());

        // 2. Completada a pendiente -> rechaza
        var resCompletada = await controller.UpdateAppointmentStatus(citaCompletada.Id, "pendiente") as RedirectToActionResult;
        Assert.Equal("No se puede cambiar el estado de una cita completada.", controller.TempData["ErrorMessage"]?.ToString());

        // 3. ChangeAppointmentStatus alias también aplica
        var resAlias = await controller.ChangeAppointmentStatus(citaCompletada.Id, "cancelada") as RedirectToActionResult;
        Assert.Equal("No se puede cambiar el estado de una cita completada.", controller.TempData["ErrorMessage"]?.ToString());
    }

    [Fact]
    public async Task DashboardAdmin_UpdateAppointmentStatus_CitaCancelada_ReactivarConCruce_Rechaza()
    {
        var controller = CrearDashboardController();
        var fecha = new DateTime(2026, 11, 23, 11, 0, 0);

        var citaActiva = new Appointment
        {
            UserId = 2,
            OptometraId = 3,
            Servicio = "Examen",
            FechaHora = fecha,
            Estado = "confirmada",
            Activo = true
        };
        var citaCancelada = new Appointment
        {
            UserId = 4,
            OptometraId = 3,
            Servicio = "Examen",
            FechaHora = fecha,
            Estado = "cancelada",
            Activo = true
        };
        _context.Appointments.AddRange(citaActiva, citaCancelada);
        await _context.SaveChangesAsync();

        var result = await controller.UpdateAppointmentStatus(citaCancelada.Id, "confirmada") as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("No es posible reactivar la cita: el horario ya está ocupado por otra cita.", controller.TempData["ErrorMessage"]?.ToString());
    }

    // ==========================================
    // REQUERIMIENTO 2: Zona Horaria Colombia
    // ==========================================

    [Fact]
    public void ZonaHoraria_ObtenerHoraActualColombia_RetornaOffsetValido()
    {
        var ahoraColombia = Appointment.ObtenerHoraActualColombia();
        var ahoraUtc = DateTime.UtcNow;

        var diffHoras = (ahoraUtc - ahoraColombia).TotalHours;
        // Colombia es UTC-5 todo el año
        Assert.InRange(diffHoras, 4.95, 5.05);

        // Prueba explícita a +1 hora y a -1 hora
        var fechaMasUnaHora = ahoraColombia.AddHours(1);
        var fechaMenosUnaHora = ahoraColombia.AddHours(-1);

        Assert.True(Appointment.EsFechaFuturaColombia(fechaMasUnaHora));
        Assert.False(Appointment.EsFechaFuturaColombia(fechaMenosUnaHora));
    }

    [Fact]
    public async Task Optometra_CreateAppointment_ValidaFechaFuturaColombia_MasUnaHora_Y_MenosUnaHora()
    {
        var controller = CrearOptometraController(3);
        var ahoraColombia = Appointment.ObtenerHoraActualColombia();

        // 1. Fecha hoy a -1 hora -> Debe ser rechazada
        var modelPasada = new AppointmentFormViewModel
        {
            UserId = 2,
            Servicio = "Examen Visual",
            FechaHora = ahoraColombia.AddHours(-1),
            Estado = "pendiente"
        };
        var resPasada = await controller.CreateAppointment(modelPasada) as RedirectToActionResult;
        Assert.NotNull(resPasada);
        Assert.Equal("La fecha y hora de la cita debe ser futura.", controller.TempData["ErrorMessage"]?.ToString());

        // 2. Fecha futura (siguiente lunes a las 10:00 AM para garantizar horario laboral)
        var proxLunes = ahoraColombia.Date.AddDays(((int)DayOfWeek.Monday - (int)ahoraColombia.DayOfWeek + 7) % 7);
        if (proxLunes <= ahoraColombia.Date) proxLunes = proxLunes.AddDays(7);
        var fechaFuturaLaboral = proxLunes.AddHours(10);

        var modelFutura = new AppointmentFormViewModel
        {
            UserId = 2,
            Servicio = "Examen Visual",
            FechaHora = fechaFuturaLaboral,
            Estado = "pendiente"
        };
        var resFutura = await controller.CreateAppointment(modelFutura) as RedirectToActionResult;
        Assert.NotNull(resFutura);
        Assert.Equal("Cita programada exitosamente.", controller.TempData["SuccessMessage"]?.ToString());
    }

    [Fact]
    public async Task Dashboard_UserScheduleAppointment_ValidaFechaFuturaColombia_MenosUnaHora()
    {
        var controller = CrearDashboardController(userId: 2, role: "usuario");
        var ahoraCol = Appointment.ObtenerHoraActualColombia();

        // Fecha hoy a -1 hora -> rechazada
        var result = await controller.UserScheduleAppointment("Examen", ahoraCol.AddHours(-1), null) as RedirectToActionResult;
        Assert.NotNull(result);
        Assert.Equal("Por favor selecciona un servicio y una fecha/hora válida a futuro.", controller.TempData["ErrorMessage"]?.ToString());
    }

    [Fact]
    public async Task Dashboard_UserRescheduleAppointment_ValidaFechaFuturaColombia_MenosUnaHora()
    {
        var controller = CrearDashboardController(userId: 2, role: "usuario");
        var cita = new Appointment
        {
            UserId = 2,
            OptometraId = 3,
            Servicio = "Examen",
            FechaHora = DateTime.UtcNow.AddDays(3),
            Estado = "pendiente",
            Activo = true
        };
        _context.Appointments.Add(cita);
        await _context.SaveChangesAsync();

        var ahoraCol = Appointment.ObtenerHoraActualColombia();

        // Reprogramar a -1 hora -> rechazada
        var result = await controller.UserRescheduleAppointment(cita.Id, ahoraCol.AddHours(-1)) as RedirectToActionResult;
        Assert.NotNull(result);
        Assert.Equal("La nueva fecha y hora debe ser a futuro.", controller.TempData["ErrorMessage"]?.ToString());
    }

    [Fact]
    public async Task Dashboard_AdminCreateAppointment_ValidaFechaFuturaColombia_MenosUnaHora()
    {
        var controller = CrearDashboardController(userId: 1, role: "admin");
        var ahoraCol = Appointment.ObtenerHoraActualColombia();

        // Admin creando cita con fecha a -1 hora -> rechazada
        var result = await controller.CreateAppointment(2, "Examen", ahoraCol.AddHours(-1), null, 3) as RedirectToActionResult;
        Assert.NotNull(result);
        Assert.Equal("La fecha y hora de la cita debe ser futura.", controller.TempData["ErrorMessage"]?.ToString());
    }

    // ==========================================
    // REQUERIMIENTO 3: Manejo Seguro de Errores (sin ex.Message)
    // ==========================================

    [Fact]
    public async Task AppointmentsApi_ManejoErrores_NoExponeExMessage()
    {
        var api = new AppointmentsApiController(_context);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "2") };
        api.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) }
        };

        // Cancelar cita inexistente retorna NotFound seguro sin ex.Message
        var resultNotFound = await api.CancelAppointment(99999) as NotFoundObjectResult;
        Assert.NotNull(resultNotFound);
        Assert.DoesNotContain("Exception", resultNotFound.Value?.ToString() ?? "");
    }

    // ==========================================
    // REQUERIMIENTO 4: OptometraController.EditAppointment
    // ==========================================

    [Fact]
    public async Task Optometra_EditAppointment_ValidaEstadoContraEstadosValidos()
    {
        var controller = CrearOptometraController(3);
        var cita = new Appointment
        {
            UserId = 2,
            OptometraId = 3,
            Servicio = "Examen Visual",
            FechaHora = DateTime.UtcNow.AddDays(2),
            Estado = "pendiente",
            Activo = true
        };
        _context.Appointments.Add(cita);
        await _context.SaveChangesAsync();

        var model = new AppointmentFormViewModel
        {
            Id = cita.Id,
            UserId = 2,
            Servicio = "Examen Visual",
            FechaHora = cita.FechaHora,
            Estado = "estado_inventado_invalido"
        };

        var result = await controller.EditAppointment(model) as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Contains("Estado no válido", controller.TempData["ErrorMessage"]?.ToString());
    }

    [Fact]
    public async Task Optometra_EditAppointment_RechazaFechaAnteriorAAhora_SiSeCambiaFecha()
    {
        var controller = CrearOptometraController(3);
        var fechaFutura = DateTime.UtcNow.AddDays(2);
        var cita = new Appointment
        {
            UserId = 2,
            OptometraId = 3,
            Servicio = "Examen Visual",
            FechaHora = fechaFutura,
            Estado = "pendiente",
            Activo = true
        };
        _context.Appointments.Add(cita);
        await _context.SaveChangesAsync();

        var ahoraCol = Appointment.ObtenerHoraActualColombia();
        var model = new AppointmentFormViewModel
        {
            Id = cita.Id,
            UserId = 2,
            Servicio = "Examen Visual",
            FechaHora = ahoraCol.AddHours(-2), // Cambiada a una fecha pasada
            Estado = "pendiente"
        };

        var result = await controller.EditAppointment(model) as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("La fecha y hora de la cita debe ser futura.", controller.TempData["ErrorMessage"]?.ToString());
    }

    [Fact]
    public async Task Optometra_EditAppointment_CitaYaEnElPasado_PermiteEditarSiFechaNoCambia()
    {
        var controller = CrearOptometraController(3);
        var ahoraCol = Appointment.ObtenerHoraActualColombia();
        // Cita lunes pasado a las 10 AM
        var fechaPasadaLaboral = ahoraCol.Date.AddDays(-7).AddHours(10);

        var citaPasada = new Appointment
        {
            UserId = 2,
            OptometraId = 3,
            Servicio = "Examen Visual",
            FechaHora = fechaPasadaLaboral,
            Estado = "pendiente",
            Notas = "Nota original",
            Activo = true
        };
        _context.Appointments.Add(citaPasada);
        await _context.SaveChangesAsync();

        // Editar nota y confirmar, manteniendo EXACTAMENTE la misma fecha pasada
        var model = new AppointmentFormViewModel
        {
            Id = citaPasada.Id,
            UserId = 2,
            Servicio = "Examen Visual",
            FechaHora = fechaPasadaLaboral,
            Estado = "confirmada",
            Notas = "Nota actualizada de cita pasada"
        };

        var result = await controller.EditAppointment(model) as RedirectToActionResult;

        Assert.NotNull(result);
        Assert.Equal("Cita actualizada exitosamente.", controller.TempData["SuccessMessage"]?.ToString());

        var citaDb = await _context.Appointments.FindAsync(citaPasada.Id);
        Assert.Equal("confirmada", citaDb!.Estado);
        Assert.Equal("Nota actualizada de cita pasada", citaDb.Notas);
    }
}
