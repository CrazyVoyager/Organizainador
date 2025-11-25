using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Organizainador.Data;
using Organizainador.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

[Authorize]
public class HorariosController : Controller
{
    private readonly AppDbContext _context;

    public HorariosController(AppDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserIdInt()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdString, out int userId) ? userId : 0;
    }

    private async Task<bool> CheckScheduleConflict(int userId, string diaSemana, TimeSpan horaInicio, TimeSpan horaFin, int? excludeHorarioId)
    {
        var userClasesIds = await _context.Clases
            .Where(c => c.UsuarioId == userId)
            .Select(c => c.Id)
            .ToListAsync();

        var userActividadesIds = await _context.Actividades
            .Where(a => a.UsuarioId == userId)
            .Select(a => a.Id)
            .ToListAsync();

        var conflictingHorarios = await _context.Horarios
            .Where(h => h.DiaSemana == diaSemana &&
                       (excludeHorarioId == null || h.Id != excludeHorarioId) &&
                       ((h.ClaseId.HasValue && userClasesIds.Contains(h.ClaseId.Value)) ||
                        (h.ActividadId.HasValue && userActividadesIds.Contains(h.ActividadId.Value))))
            .ToListAsync();

        foreach (var horario in conflictingHorarios)
        {
            if ((horaInicio >= horario.HoraInicio && horaInicio < horario.HoraFin) ||
                (horaFin > horario.HoraInicio && horaFin <= horario.HoraFin) ||
                (horaInicio <= horario.HoraInicio && horaFin >= horario.HoraFin))
            {
                return true;
            }
        }

        return false;
    }

    public async Task<IActionResult> Index()
    {
        int userId = GetCurrentUserIdInt();
        if (userId == 0) return Forbid();

        var userClasesIds = await _context.Clases
            .Where(c => c.UsuarioId == userId)
            .Select(c => c.Id)
            .ToListAsync();

        var userActividadesIds = await _context.Actividades
            .Where(a => a.UsuarioId == userId)
            .Select(a => a.Id)
            .ToListAsync();

        var horarios = await _context.Horarios
            .Include(h => h.Clase)
            .Include(h => h.Actividad)
            .Where(h => (h.ClaseId.HasValue && userClasesIds.Contains(h.ClaseId.Value)) ||
                       (h.ActividadId.HasValue && userActividadesIds.Contains(h.ActividadId.Value)))
            .ToListAsync();

        if (!userClasesIds.Any() && !userActividadesIds.Any())
        {
            TempData["InfoMessage"] = "Aún no tienes clases ni actividades registradas, por favor registra una para agregarle horarios.";
        }

        return View(horarios);
    }

    public async Task<IActionResult> Create()
    {
        int userId = GetCurrentUserIdInt();
        if (userId == 0) return Forbid();

        var userClases = await _context.Clases
            .Where(c => c.UsuarioId == userId)
            .ToListAsync();

        var userActividades = await _context.Actividades
            .Where(a => a.UsuarioId == userId)
            .ToListAsync();

        if (!userClases.Any() && !userActividades.Any())
        {
            TempData["ErrorMessage"] = "Debes crear una Clase o Actividad primero para poder asignarle un Horario.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["ClaseId"] = new SelectList(userClases, "Id", "Nombre");
        ViewData["ActividadId"] = new SelectList(userActividades, "Id", "Nombre");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ClaseId,ActividadId,DiaSemana,HoraInicio,HoraFin,EsRecurrente")] HorarioModel horarioModel)
    {
        int userId = GetCurrentUserIdInt();
        if (userId == 0) return Forbid();

        if (!horarioModel.ClaseId.HasValue && !horarioModel.ActividadId.HasValue)
        {
            ModelState.AddModelError("", "Debes seleccionar una Clase o una Actividad.");
        }

        if (horarioModel.ClaseId.HasValue && horarioModel.ActividadId.HasValue)
        {
            ModelState.AddModelError("", "Debes seleccionar solo una Clase o una Actividad, no ambas.");
        }

        if (horarioModel.ClaseId.HasValue)
        {
            var clase = await _context.Clases.FindAsync(horarioModel.ClaseId.Value);
            if (clase == null || clase.UsuarioId != userId)
            {
                return Forbid();
            }
        }

        if (horarioModel.ActividadId.HasValue)
        {
            var actividad = await _context.Actividades.FindAsync(horarioModel.ActividadId.Value);
            if (actividad == null || actividad.UsuarioId != userId)
            {
                return Forbid();
            }
        }

        var hasConflict = await CheckScheduleConflict(
            userId,
            horarioModel.DiaSemana,
            horarioModel.HoraInicio,
            horarioModel.HoraFin,
            null
        );

        if (hasConflict)
        {
            ModelState.AddModelError("", "El rango de horas seleccionado ya está ocupado por otra actividad o clase en ese día.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(horarioModel);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Horario creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        var userClases = await _context.Clases
            .Where(c => c.UsuarioId == userId)
            .ToListAsync();

        var userActividades = await _context.Actividades
            .Where(a => a.UsuarioId == userId)
            .ToListAsync();

        ViewData["ClaseId"] = new SelectList(userClases, "Id", "Nombre", horarioModel.ClaseId);
        ViewData["ActividadId"] = new SelectList(userActividades, "Id", "Nombre", horarioModel.ActividadId);
        return View(horarioModel);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        int userId = GetCurrentUserIdInt();
        if (userId == 0) return Forbid();

        var horarioModel = await _context.Horarios
            .Include(h => h.Clase)
            .Include(h => h.Actividad)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (horarioModel == null)
        {
            return NotFound();
        }

        var belongsToUser = false;
        if (horarioModel.ClaseId.HasValue)
        {
            belongsToUser = horarioModel.Clase?.UsuarioId == userId;
        }
        else if (horarioModel.ActividadId.HasValue)
        {
            belongsToUser = horarioModel.Actividad?.UsuarioId == userId;
        }

        if (!belongsToUser)
        {
            return NotFound();
        }

        var userClases = await _context.Clases
            .Where(c => c.UsuarioId == userId)
            .ToListAsync();

        var userActividades = await _context.Actividades
            .Where(a => a.UsuarioId == userId)
            .ToListAsync();

        ViewData["ClaseId"] = new SelectList(userClases, "Id", "Nombre", horarioModel.ClaseId);
        ViewData["ActividadId"] = new SelectList(userActividades, "Id", "Nombre", horarioModel.ActividadId);
        return View(horarioModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ClaseId,ActividadId,DiaSemana,HoraInicio,HoraFin,EsRecurrente")] HorarioModel horarioModel)
    {
        if (id != horarioModel.Id) return NotFound();

        int userId = GetCurrentUserIdInt();
        if (userId == 0) return Forbid();

        var existingHorario = await _context.Horarios
            .Include(h => h.Clase)
            .Include(h => h.Actividad)
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id);

        if (existingHorario == null)
        {
            return NotFound();
        }

        var belongsToUser = false;
        if (existingHorario.ClaseId.HasValue)
        {
            belongsToUser = existingHorario.Clase?.UsuarioId == userId;
        }
        else if (existingHorario.ActividadId.HasValue)
        {
            belongsToUser = existingHorario.Actividad?.UsuarioId == userId;
        }

        if (!belongsToUser)
        {
            return NotFound();
        }

        if (!horarioModel.ClaseId.HasValue && !horarioModel.ActividadId.HasValue)
        {
            ModelState.AddModelError("", "Debes seleccionar una Clase o una Actividad.");
        }

        if (horarioModel.ClaseId.HasValue && horarioModel.ActividadId.HasValue)
        {
            ModelState.AddModelError("", "Debes seleccionar solo una Clase o una Actividad, no ambas.");
        }

        if (horarioModel.ClaseId.HasValue)
        {
            var newClase = await _context.Clases.FindAsync(horarioModel.ClaseId.Value);
            if (newClase == null || newClase.UsuarioId != userId)
            {
                ModelState.AddModelError("ClaseId", "La clase seleccionada no es válida o no te pertenece.");
            }
        }

        if (horarioModel.ActividadId.HasValue)
        {
            var newActividad = await _context.Actividades.FindAsync(horarioModel.ActividadId.Value);
            if (newActividad == null || newActividad.UsuarioId != userId)
            {
                ModelState.AddModelError("ActividadId", "La actividad seleccionada no es válida o no te pertenece.");
            }
        }

        var hasConflict = await CheckScheduleConflict(
            userId,
            horarioModel.DiaSemana,
            horarioModel.HoraInicio,
            horarioModel.HoraFin,
            id
        );

        if (hasConflict)
        {
            ModelState.AddModelError("", "El rango de horas seleccionado ya está ocupado por otra actividad o clase en ese día.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(horarioModel);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Horario actualizado exitosamente.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HorarioModelExists(horarioModel.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        var userClases = await _context.Clases
            .Where(c => c.UsuarioId == userId)
            .ToListAsync();

        var userActividades = await _context.Actividades
            .Where(a => a.UsuarioId == userId)
            .ToListAsync();

        ViewData["ClaseId"] = new SelectList(userClases, "Id", "Nombre", horarioModel.ClaseId);
        ViewData["ActividadId"] = new SelectList(userActividades, "Id", "Nombre", horarioModel.ActividadId);
        return View(horarioModel);
    }

    // Cambiado de "Eliminar" a "Delete"
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        int userId = GetCurrentUserIdInt();
        if (userId == 0) return Forbid();

        var horarioModel = await _context.Horarios
            .Include(h => h.Clase)
            .Include(h => h.Actividad)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (horarioModel == null)
        {
            return NotFound();
        }

        var belongsToUser = false;
        if (horarioModel.ClaseId.HasValue)
        {
            belongsToUser = horarioModel.Clase?.UsuarioId == userId;
        }
        else if (horarioModel.ActividadId.HasValue)
        {
            belongsToUser = horarioModel.Actividad?.UsuarioId == userId;
        }

        if (!belongsToUser)
        {
            return NotFound();
        }

        return View(horarioModel);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        int userId = GetCurrentUserIdInt();
        if (userId == 0) return Forbid();

        var horarioModel = await _context.Horarios
            .Include(h => h.Clase)
            .Include(h => h.Actividad)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (horarioModel == null)
        {
            TempData["ErrorMessage"] = "No se pudo encontrar este Horario.";
            return RedirectToAction(nameof(Index));
        }

        var belongsToUser = false;
        if (horarioModel.ClaseId.HasValue)
        {
            belongsToUser = horarioModel.Clase?.UsuarioId == userId;
        }
        else if (horarioModel.ActividadId.HasValue)
        {
            belongsToUser = horarioModel.Actividad?.UsuarioId == userId;
        }

        if (!belongsToUser)
        {
            TempData["ErrorMessage"] = "No tienes permiso para eliminar este Horario.";
            return RedirectToAction(nameof(Index));
        }

        _context.Horarios.Remove(horarioModel);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Horario eliminado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    private bool HorarioModelExists(int id)
    {
        return _context.Horarios.Any(e => e.Id == id);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        int userId = GetCurrentUserIdInt();
        if (userId == 0) return Forbid();

        var horarioModel = await _context.Horarios
            .Include(h => h.Clase)
            .Include(h => h.Actividad)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (horarioModel == null)
        {
            return NotFound();
        }

        var belongsToUser = false;
        if (horarioModel.ClaseId.HasValue)
        {
            belongsToUser = horarioModel.Clase?.UsuarioId == userId;
        }
        else if (horarioModel.ActividadId.HasValue)
        {
            belongsToUser = horarioModel.Actividad?.UsuarioId == userId;
        }

        if (!belongsToUser)
        {
            return NotFound();
        }

        return View(horarioModel);
    }
}