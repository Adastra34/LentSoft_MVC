using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Controllers;
using LentSoft.Web.Controllers.Api;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
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
}
