using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Services;

public class PqrsService : IPqrsService
{
    private readonly LentSoftDbContext _context;

    public PqrsService(LentSoftDbContext context)
    {
        _context = context;
    }

    public async Task<Pqrs> CrearAsync(Pqrs pqrs)
    {
        if (pqrs == null)
        {
            throw new ArgumentNullException(nameof(pqrs));
        }

        if (string.IsNullOrWhiteSpace(pqrs.Estado))
        {
            pqrs.Estado = "Pendiente";
        }

        if (pqrs.FechaCreacion == default)
        {
            pqrs.FechaCreacion = DateTime.UtcNow;
        }

        _context.Pqrs.Add(pqrs);
        await _context.SaveChangesAsync();

        if (string.IsNullOrWhiteSpace(pqrs.Radicado))
        {
            pqrs.Radicado = $"PQRS-{pqrs.FechaCreacion:yyyyMMdd}-{pqrs.Id:D4}";
            await _context.SaveChangesAsync();
        }

        return pqrs;
    }

    public async Task<Pqrs> CrearRadicadoAsync(int userId, string tipo, string asunto, string mensaje)
    {
        var pqrs = new Pqrs
        {
            UserId = userId,
            Tipo = tipo,
            Asunto = asunto,
            Mensaje = mensaje,
            Estado = "Pendiente",
            FechaCreacion = DateTime.UtcNow
        };

        return await CrearAsync(pqrs);
    }

    public async Task<List<Pqrs>> ObtenerPorUsuarioAsync(int userId)
    {
        return await _context.Pqrs
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.FechaCreacion)
            .ToListAsync();
    }

    public Task<List<Pqrs>> ListarPorUsuarioAsync(int userId) => ObtenerPorUsuarioAsync(userId);

    public async Task<List<Pqrs>> ObtenerTodosAsync(string? estado = null)
    {
        var query = _context.Pqrs
            .Include(p => p.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(p => p.Estado == estado);
        }

        return await query
            .OrderByDescending(p => p.FechaCreacion)
            .ToListAsync();
    }

    public Task<List<Pqrs>> ListarTodosAsync(string? estado = null) => ObtenerTodosAsync(estado);

    public async Task<Pqrs?> ObtenerPorIdAsync(int id)
    {
        return await _context.Pqrs
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<bool> ActualizarEstadoAsync(int id, string nuevoEstado)
    {
        var item = await _context.Pqrs.FirstOrDefaultAsync(p => p.Id == id);
        if (item == null)
        {
            return false;
        }

        item.Estado = nuevoEstado;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResponderAsync(int id, string respuestaAdmin, string nuevoEstado = "Resuelto")
    {
        var item = await _context.Pqrs.FirstOrDefaultAsync(p => p.Id == id);
        if (item == null)
        {
            return false;
        }

        item.RespuestaAdmin = respuestaAdmin;
        item.FechaRespuesta = DateTime.UtcNow;
        item.Estado = string.IsNullOrWhiteSpace(nuevoEstado) ? "Resuelto" : nuevoEstado;

        await _context.SaveChangesAsync();
        return true;
    }
}
