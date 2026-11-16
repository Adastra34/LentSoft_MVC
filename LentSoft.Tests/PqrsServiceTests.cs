using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Services;
using Xunit;

namespace LentSoft.Tests;

public class PqrsServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LentSoftDbContext _context;
    private readonly PqrsService _service;

    public PqrsServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LentSoftDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LentSoftDbContext(options);
        _context.Database.EnsureCreated();

        _service = new PqrsService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CrearAsync_AsignaEstadoPendienteYRadicadoInicial()
    {
        // Arrange (Usuario 2 ya existe en seed data: user@lentsoft.com)
        var nuevaPqrs = new Pqrs
        {
            UserId = 2,
            Tipo = "Petición",
            Asunto = "Consulta sobre entrega de montura",
            Mensaje = "Deseo conocer la fecha estimada de entrega de mi pedido."
        };

        // Act
        var resultado = await _service.CrearAsync(nuevaPqrs);

        // Assert
        Assert.NotNull(resultado);
        Assert.True(resultado.Id > 0);
        Assert.Equal("Pendiente", resultado.Estado);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Radicado));
        Assert.StartsWith("PQRS-", resultado.Radicado);
        Assert.Contains(resultado.Id.ToString("D4"), resultado.Radicado);
        Assert.NotEqual(default, resultado.FechaCreacion);
        Assert.Null(resultado.RespuestaAdmin);
        Assert.Null(resultado.FechaRespuesta);
    }

    [Fact]
    public async Task CrearRadicadoAsync_PersisteDatosCorrectamente()
    {
        // Act
        var resultado = await _service.CrearRadicadoAsync(
            userId: 2,
            tipo: "Reclamo",
            asunto: "Lente con graduación incorrecta",
            mensaje: "El lente izquierdo no coincide con la fórmula enviada por mi optómetra."
        );

        // Assert
        Assert.NotNull(resultado);
        var guardado = await _context.Pqrs.FindAsync(resultado.Id);
        Assert.NotNull(guardado);
        Assert.Equal(2, guardado.UserId);
        Assert.Equal("Reclamo", guardado.Tipo);
        Assert.Equal("Lente con graduación incorrecta", guardado.Asunto);
        Assert.Equal("El lente izquierdo no coincide con la fórmula enviada por mi optómetra.", guardado.Mensaje);
        Assert.Equal("Pendiente", guardado.Estado);
    }

    [Fact]
    public async Task ObtenerPorUsuarioAsync_FiltraExclusivamentePqrsDelUsuario()
    {
        // Arrange (Usuario 1 y Usuario 2 existen en seed)
        await _service.CrearRadicadoAsync(1, "Queja", "Demora en atención", "Demora de 45 minutos.");
        await _service.CrearRadicadoAsync(1, "Solicitud", "Copia de factura", "Requiero copia de la factura.");
        await _service.CrearRadicadoAsync(2, "Petición", "Horarios de atención", "Deseo saber si atienden sábados.");

        // Act
        var listaUsuario1 = await _service.ObtenerPorUsuarioAsync(1);
        var listaUsuario2 = await _service.ObtenerPorUsuarioAsync(2);

        // Assert
        Assert.Equal(2, listaUsuario1.Count);
        Assert.All(listaUsuario1, p => Assert.Equal(1, p.UserId));
        Assert.Single(listaUsuario2);
        Assert.Equal(2, listaUsuario2.First().UserId);
    }

    [Fact]
    public async Task ObtenerTodosAsync_PermiteListarTodoYFiltrarPorEstado()
    {
        // Arrange
        var p1 = await _service.CrearRadicadoAsync(1, "Petición", "Caso 1", "Mensaje 1");
        var p2 = await _service.CrearRadicadoAsync(2, "Queja", "Caso 2", "Mensaje 2");
        await _service.ResponderAsync(p2.Id, "Respuesta al caso 2", "Resuelto");

        // Act
        var todos = await _service.ObtenerTodosAsync();
        var soloPendientes = await _service.ObtenerTodosAsync("Pendiente");
        var soloResueltos = await _service.ObtenerTodosAsync("Resuelto");

        // Assert
        Assert.Equal(2, todos.Count);
        Assert.Single(soloPendientes);
        Assert.Equal(p1.Id, soloPendientes.First().Id);
        Assert.Single(soloResueltos);
        Assert.Equal(p2.Id, soloResueltos.First().Id);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_RetornaPqrsCorrectaOSiNoExisteNull()
    {
        // Arrange
        var creada = await _service.CrearRadicadoAsync(2, "Solicitud", "Cambio de dirección", "Nueva dirección de envío");

        // Act
        var encontrada = await _service.ObtenerPorIdAsync(creada.Id);
        var noExiste = await _service.ObtenerPorIdAsync(999999);

        // Assert
        Assert.NotNull(encontrada);
        Assert.Equal(creada.Id, encontrada.Id);
        Assert.Equal("Cambio de dirección", encontrada.Asunto);
        Assert.Null(noExiste);
    }

    [Fact]
    public async Task ResponderAsync_ActualizaRespuestaFechaYEstadoAdministrativo()
    {
        // Arrange
        var pqrs = await _service.CrearRadicadoAsync(2, "Queja", "Mala atención en sucursal", "El asesor no me atendió bien.");
        var respuestaTexto = "Lamentamos lo sucedido. Hemos tomado medidas disciplinarias con el personal.";

        // Act
        var exito = await _service.ResponderAsync(pqrs.Id, respuestaTexto, "Resuelto");

        // Assert
        Assert.True(exito);
        var actualizada = await _context.Pqrs.FindAsync(pqrs.Id);
        Assert.NotNull(actualizada);
        Assert.Equal(respuestaTexto, actualizada.RespuestaAdmin);
        Assert.NotNull(actualizada.FechaRespuesta);
        Assert.Equal("Resuelto", actualizada.Estado);
    }

    [Fact]
    public async Task ResponderAsync_IdInexistente_RetornaFalse()
    {
        // Act
        var exito = await _service.ResponderAsync(88888, "Respuesta a caso inexistente", "Resuelto");

        // Assert
        Assert.False(exito);
    }

    [Fact]
    public async Task ActualizarEstadoAsync_CambiaEstadoSatisfactoriamente()
    {
        // Arrange
        var pqrs = await _service.CrearRadicadoAsync(2, "Reclamo", "Garantía de marco", "El marco se desajustó.");

        // Act
        var exito = await _service.ActualizarEstadoAsync(pqrs.Id, "En Proceso");

        // Assert
        Assert.True(exito);
        var actualizada = await _context.Pqrs.FindAsync(pqrs.Id);
        Assert.NotNull(actualizada);
        Assert.Equal("En Proceso", actualizada.Estado);
    }
}
