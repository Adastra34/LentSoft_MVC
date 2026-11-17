using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public interface IPqrsService
{
    Task<Pqrs> CrearAsync(Pqrs pqrs);
    Task<Pqrs> CrearRadicadoAsync(int userId, string tipo, string asunto, string mensaje);
    Task<List<Pqrs>> ObtenerPorUsuarioAsync(int userId);
    Task<List<Pqrs>> ListarPorUsuarioAsync(int userId);
    Task<List<Pqrs>> ObtenerTodosAsync(string? estado = null);
    Task<List<Pqrs>> ListarTodosAsync(string? estado = null);
    Task<Pqrs?> ObtenerPorIdAsync(int id);
    Task<bool> ActualizarEstadoAsync(int id, string nuevoEstado);
    Task<bool> ResponderAsync(int id, string respuestaAdmin, string nuevoEstado = "Resuelto");
}
