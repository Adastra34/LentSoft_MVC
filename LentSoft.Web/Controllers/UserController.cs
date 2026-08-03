using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;

namespace LentSoft.Web.Controllers;

[Authorize(Roles = "admin")]
public class UserController : Controller
{
    private readonly LentSoftDbContext _context;

    public UserController(LentSoftDbContext context)
    {
        _context = context;
    }

    // ── CLIENTES CRUD ──

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateClient(string Nombre, string Apellido, string Email, string? Telefono, string TipoDocumento, string NumeroDocumento, string? Password)
    {
        if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(Email))
        {
            TempData["ErrorMessage"] = "El nombre y el correo son obligatorios.";
            return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
        }

        if (await _context.Users.AnyAsync(u => u.Email == Email))
        {
            TempData["ErrorMessage"] = "El correo electrónico ya está registrado.";
            return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
        }

        var pass = string.IsNullOrWhiteSpace(Password) ? "user123" : Password;
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(pass);

        var user = new User
        {
            Nombre = Nombre.Trim(),
            Apellido = string.IsNullOrWhiteSpace(Apellido) ? "" : Apellido.Trim(),
            Email = Email.Trim().ToLower(),
            Telefono = Telefono?.Trim(),
            TipoDocumento = string.IsNullOrWhiteSpace(TipoDocumento) ? "CC" : TipoDocumento,
            NumeroDocumento = string.IsNullOrWhiteSpace(NumeroDocumento) ? Guid.NewGuid().ToString("N").Substring(0, 10) : NumeroDocumento.Trim(),
            PasswordHash = passwordHash,
            Role = "usuario",
            FechaRegistro = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cliente creado exitosamente.";
        return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditClient(int id, string Nombre, string Apellido, string Email, string? Telefono)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Cliente no encontrado.";
            return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
        }

        if (await _context.Users.AnyAsync(u => u.Email == Email && u.Id != id))
        {
            TempData["ErrorMessage"] = "El correo electrónico ya pertenece a otro usuario.";
            return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
        }

        user.Nombre = Nombre.Trim();
        user.Apellido = string.IsNullOrWhiteSpace(Apellido) ? "" : Apellido.Trim();
        user.Email = Email.Trim().ToLower();
        user.Telefono = Telefono?.Trim();

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cliente actualizado exitosamente.";
        return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Usuario no encontrado.";
            return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
        }

        // Regla de Negocio RF-M07-03: No permitir eliminar al único usuario con rol "admin" restante
        if (user.Role.ToLower() == "admin")
        {
            var adminCount = await _context.Users.CountAsync(u => u.Role.ToLower() == "admin");
            if (adminCount <= 1)
            {
                TempData["ErrorMessage"] = "No se puede eliminar el único usuario administrador restante del sistema.";
                return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
            }
        }

        user.Activo = false;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Usuario eliminado exitosamente.";
        return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivateUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Usuario no encontrado.";
            return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
        }

        user.Activo = true;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Usuario reactivado exitosamente.";
        return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
    }


    // ── TRABAJADORES (EMPLEADOS) CRUD ──

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmployee(string Nombre, string Email, string? Telefono, string Puesto, string Departamento, decimal Salario, string Rol)
    {
        if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(Email))
        {
            TempData["ErrorMessage"] = "El nombre y el correo son obligatorios.";
            return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
        }

        if (await _context.Employees.AnyAsync(e => e.Email == Email))
        {
            TempData["ErrorMessage"] = "Ya existe un empleado registrado con ese correo electrónico.";
            return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
        }

        var employee = new Employee
        {
            Nombre = Nombre.Trim(),
            Email = Email.Trim().ToLower(),
            Telefono = Telefono?.Trim(),
            Puesto = string.IsNullOrWhiteSpace(Puesto) ? "Empleado" : Puesto.Trim(),
            Departamento = string.IsNullOrWhiteSpace(Departamento) ? "General" : Departamento.Trim(),
            Salario = Salario < 0 ? 0 : Salario,
            Rol = string.IsNullOrWhiteSpace(Rol) ? "Trabajador" : Rol.Trim(),
            FechaContratacion = DateTime.UtcNow,
            Activo = true
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Trabajador registrado exitosamente.";
        return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEmployee(int id, string Nombre, string Email, string? Telefono, string Puesto, string Departamento, decimal Salario, string Rol, bool Activo)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            TempData["ErrorMessage"] = "Trabajador no encontrado.";
            return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
        }

        if (await _context.Employees.AnyAsync(e => e.Email == Email && e.Id != id))
        {
            TempData["ErrorMessage"] = "El correo electrónico ya pertenece a otro trabajador.";
            return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
        }

        employee.Nombre = Nombre.Trim();
        employee.Email = Email.Trim().ToLower();
        employee.Telefono = Telefono?.Trim();
        employee.Puesto = string.IsNullOrWhiteSpace(Puesto) ? employee.Puesto : Puesto.Trim();
        employee.Departamento = string.IsNullOrWhiteSpace(Departamento) ? employee.Departamento : Departamento.Trim();
        employee.Salario = Salario < 0 ? 0 : Salario;
        employee.Rol = string.IsNullOrWhiteSpace(Rol) ? "Trabajador" : Rol.Trim();
        employee.Activo = Activo;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Datos del trabajador actualizados exitosamente.";
        return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            TempData["ErrorMessage"] = "Trabajador no encontrado.";
            return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
        }

        employee.Activo = false;
        _context.Employees.Update(employee);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Trabajador eliminado exitosamente.";
        return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivateEmployee(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            TempData["ErrorMessage"] = "Trabajador no encontrado.";
            return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
        }

        employee.Activo = true;
        _context.Employees.Update(employee);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Trabajador reactivado exitosamente.";
        return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
    }
}
