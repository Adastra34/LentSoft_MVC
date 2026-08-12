using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LentSoft.Web.Data;
using LentSoft.Web.Models.Entities;
using LentSoft.Web.Models.ViewModels;

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
    public async Task<IActionResult> CreateClient(ClientFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del formulario no válidos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
        }

        try
        {
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                TempData["ErrorMessage"] = "El correo electrónico ya está registrado.";
                return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
            }

            var pass = string.IsNullOrWhiteSpace(model.Password) ? "user123" : model.Password;
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(pass);

            var user = new User
            {
                Nombre = model.Nombre.Trim(),
                Apellido = string.IsNullOrWhiteSpace(model.Apellido) ? "" : model.Apellido.Trim(),
                Email = model.Email.Trim().ToLower(),
                Telefono = model.Telefono?.Trim(),
                TipoDocumento = string.IsNullOrWhiteSpace(model.TipoDocumento) ? "CC" : model.TipoDocumento,
                NumeroDocumento = string.IsNullOrWhiteSpace(model.NumeroDocumento) ? Guid.NewGuid().ToString("N").Substring(0, 10) : model.NumeroDocumento.Trim(),
                PasswordHash = passwordHash,
                Role = "usuario",
                FechaRegistro = DateTime.UtcNow,
                Activo = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cliente creado exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al crear el cliente: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditClient(ClientFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del formulario no válidos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
        }

        try
        {
            var user = await _context.Users.FindAsync(model.Id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Cliente no encontrado.";
                return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
            }

            if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != model.Id))
            {
                TempData["ErrorMessage"] = "El correo electrónico ya pertenece a otro usuario.";
                return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
            }

            user.Nombre = model.Nombre.Trim();
            user.Apellido = string.IsNullOrWhiteSpace(model.Apellido) ? "" : model.Apellido.Trim();
            user.Email = model.Email.Trim().ToLower();
            user.Telefono = model.Telefono?.Trim();

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cliente actualizado exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al actualizar el cliente: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
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
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al eliminar el usuario: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivateUser(int id)
    {
        try
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
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al reactivar el usuario: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "clientes" });
    }


    // ── TRABAJADORES (EMPLEADOS) CRUD ──

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmployee(EmployeeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del formulario no válidos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
        }

        try
        {
            if (await _context.Employees.AnyAsync(e => e.Email == model.Email))
            {
                TempData["ErrorMessage"] = "Ya existe un empleado registrado con ese correo electrónico.";
                return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
            }

            var employee = new Employee
            {
                Nombre = model.Nombre.Trim(),
                Email = model.Email.Trim().ToLower(),
                Telefono = model.Telefono?.Trim(),
                Puesto = string.IsNullOrWhiteSpace(model.Puesto) ? "Empleado" : model.Puesto.Trim(),
                Departamento = string.IsNullOrWhiteSpace(model.Departamento) ? "General" : model.Departamento.Trim(),
                Salario = model.Salario < 0 ? 0 : model.Salario,
                Rol = string.IsNullOrWhiteSpace(model.Rol) ? "Trabajador" : model.Rol.Trim(),
                FechaContratacion = DateTime.UtcNow,
                Activo = true
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Trabajador registrado exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al registrar el trabajador: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEmployee(EmployeeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Datos del formulario no válidos.";
            TempData["ErrorMessage"] = firstError;
            return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
        }

        try
        {
            var employee = await _context.Employees.FindAsync(model.Id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Trabajador no encontrado.";
                return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
            }

            if (await _context.Employees.AnyAsync(e => e.Email == model.Email && e.Id != model.Id))
            {
                TempData["ErrorMessage"] = "El correo electrónico ya pertenece a otro trabajador.";
                return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
            }

            employee.Nombre = model.Nombre.Trim();
            employee.Email = model.Email.Trim().ToLower();
            employee.Telefono = model.Telefono?.Trim();
            employee.Puesto = string.IsNullOrWhiteSpace(model.Puesto) ? employee.Puesto : model.Puesto.Trim();
            employee.Departamento = string.IsNullOrWhiteSpace(model.Departamento) ? employee.Departamento : model.Departamento.Trim();
            employee.Salario = model.Salario < 0 ? 0 : model.Salario;
            employee.Rol = string.IsNullOrWhiteSpace(model.Rol) ? "Trabajador" : model.Rol.Trim();
            employee.Activo = model.Activo;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Datos del trabajador actualizados exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al actualizar el trabajador: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        try
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
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al eliminar el trabajador: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivateEmployee(int id)
    {
        try
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
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error al reactivar el trabajador: {ex.Message}";
        }

        return RedirectToAction("Admin", "Dashboard", new { section = "usuarios", subtab = "trabajadores" });
    }
}
