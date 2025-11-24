using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Organizainador.Data;
using Organizainador.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; // Necesario para ClaimTypes
using Microsoft.AspNetCore.Mvc.Rendering; // Necesario para SelectList

[Authorize] // Asegura que solo usuarios autenticados puedan acceder
public class HorariosController : Controller
{
    private readonly AppDbContext _context;

    public HorariosController(AppDbContext context)
    {
        _context = context;
    }

    // Helper para obtener el ID de usuario autenticado como entero
    private int GetCurrentUserIdInt()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // En una app real, el ID de usuario de Identity es un string, pero
        // asumiremos que tu FK en tus modelos (ClaseModel.UsuarioId) es un INT.
        // Aquí simulamos la conversión.
        return int.TryParse(userIdString, out int userId) ? userId : 0;
    }

    // Helper para verificar conflictos de horario
    private async Task<bool> CheckScheduleConflict(int userId, string diaSemana, TimeSpan horaInicio, TimeSpan horaFin, int? excludeHorarioId)
    {
        // Obtener los IDs de todas las clases del usuario
        var userClasesIds = await _context.Clases
            .Where(c => c.UsuarioId == userId)
            .Select(c => c.Id)
            .ToListAsync();

        // Obtener los IDs de todas las actividades del usuario
        var userActividadesIds = await _context.Actividades
            .Where(a => a.UsuarioId == userId)
            .Select(a => a.Id)
            .ToListAsync();

        // Buscar horarios que se superpongan en el mismo día
        var conflictingHorarios = await _context.Horarios
            .Where(h => h.DiaSemana == diaSemana && 
                       (excludeHorarioId == null || h.Id != excludeHorarioId) &&
                       ((userClasesIds.Contains(h.ClaseId ?? 0)) || 
                        (userActividadesIds.Contains(h.ActividadId ?? 0))))
            .ToListAsync();

        // Verificar si hay superposición de horarios
        foreach (var horario in conflictingHorarios)
        {
            // Hay conflicto si:
            // - El nuevo horario comienza durante un horario existente
            // - El nuevo horario termina durante un horario existente
            // - El nuevo horario envuelve completamente un horario existente
            if ((horaInicio >= horario.HoraInicio && horaInicio < horario.HoraFin) ||
                (horaFin > horario.HoraInicio && horaFin <= horario.HoraFin) ||
                (horaInicio <= horario.HoraInicio && horaFin >= horario.HoraFin))
            {
                return true;
            }
        }

        return false;
    }

    // ======================== LISTADO PRINCIPAL (Index) ========================
    public async Task<IActionResult> Index()
    {
        int userId = GetCurrentUserIdInt();
        if (userId == 0) return Forbid();

        // 1. Obtener los IDs de todas las clases que pertenecen al usuario actual
        var userClasesIds = await _context.Clases
            .Where(c => c.UsuarioId == userId)
            .Select(c => c.Id)
            .ToListAsync();

        // 2. Obtener los IDs de todas las actividades que pertenecen al usuario actual
        var userActividadesIds = await _context.Actividades
            .Where(a => a.UsuarioId == userId)
            .Select(a => a.Id)
            .ToListAsync();

        // 3. Obtener los horarios cuyas Clases o Actividades pertenecen al usuario actual
        var horarios = await _context.Horarios
            .Include(h => h.Clase) // Incluir el objeto Clase para mostrar su nombre
            .Include(h => h.Actividad) // Incluir el objeto Actividad para mostrar su nombre
            .Where(h => (h.ClaseId.HasValue && userClasesIds.Contains(h.ClaseId.Value)) ||
                       (h.ActividadId.HasValue && userActividadesIds.Contains(h.ActividadId.Value)))
            .ToListAsync();

        // Si no hay clases ni actividades registradas para el usuario, tampoco hay horarios.
        if (!userClasesIds.Any() && !userActividadesIds.Any()) 
        {
            TempData["InfoMessage"] = "Aún no tienes clases ni actividades registradas, por favor registra una para agregarle horarios.";
        }

        return View(horarios);
    }

    // ======================== CREAR HORARIO (GET) ========================
    public async Task<IActionResult> Create()
    {
        int userId = GetCurrentUserIdInt();
        if (userId == 0) return Forbid();

        // Filtrar el dropdown para mostrar SOLO las clases del usuario actual
        var userClases = await _context.Clases
            .Where(c => c.UsuarioId == userId)
            .ToListAsync();

        // Filtrar actividades del usuario actual
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

    // ======================== CREAR HORARIO (POST) ========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ClaseId,ActividadId,DiaSemana,HoraInicio,HoraFin")] HorarioModel horarioModel)
    {
        int userId = GetCurrentUserIdInt();
        if (userId == 0) return Forbid();

        // Validar que se seleccione al menos una (Clase o Actividad)
        if (!horarioModel.ClaseId.HasValue && !horarioModel.ActividadId.HasValue)
        {
            ModelState.AddModelError("", "Debes seleccionar una Clase o una Actividad.");
        }

        // Validar que no se seleccionen ambas
        if (horarioModel.ClaseId.HasValue && horarioModel.ActividadId.HasValue)
        {
            ModelState.AddModelError("", "Debes seleccionar solo una Clase o una Actividad, no ambas.");
        }

        // Validar si la ClaseId enviada realmente pertenece al usuario actual (Seguridad)
        if (horarioModel.ClaseId.HasValue)
        {
            var clase = await _context.Clases.FindAsync(horarioModel.ClaseId.Value);
            if (clase == null || clase.UsuarioId != userId)
            {
                return Forbid(); // Acceso no autorizado o clase inexistente
            }
        }

        // Validar si la ActividadId enviada realmente pertenece al usuario actual (Seguridad)
        if (horarioModel.ActividadId.HasValue)
        {
            var actividad = await _context.Actividades.FindAsync(horarioModel.ActividadId.Value);
            if (actividad == null || actividad.UsuarioId != userId)
            {
                return Forbid(); // Acceso no autorizado o actividad inexistente
            }
        }

        // Validar conflictos de horario
        var hasConflict = await CheckScheduleConflict(
            userId, 
            horarioModel.DiaSemana, 
            horarioModel.HoraInicio, 
            horarioModel.HoraFin, 
            null // No hay ID existente al crear
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

        // Recargar los dropdowns si hay errores de validación
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

    // ======================== MODIFICAR HORARIO (GET) ========================
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        int userId = GetCurrentUserIdInt();
        if (userId == 0) return Forbid();
        
        var horarioModel = await _context.Horarios
            .Include(h => h.Clase) // Para filtrar por el ID del usuario
            .Include(h => h.Actividad)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (horarioModel == null)
        {
            return NotFound(); // No existe
        }

        // Verificar que el horario pertenece al usuario (validar por Clase o Actividad)
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
            return NotFound(); // No pertenece al usuario
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

    // ======================== MODIFICAR HORARIO (POST) ========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ClaseId,ActividadId,DiaSemana,HoraInicio,HoraFin")] HorarioModel horarioModel)
    {
        if (id != horarioModel.Id) return NotFound();

        int userId = GetCurrentUserIdInt();
        if (userId == 0) return Forbid();

        // Validar si el horario a modificar realmente pertenece a una clase o actividad del usuario actual
        var existingHorario = await _context.Horarios
            .Include(h => h.Clase)
            .Include(h => h.Actividad)
            .AsNoTracking() // Importante para que EF no intente trackear dos entidades con el mismo ID
            .FirstOrDefaultAsync(h => h.Id == id);

        if (existingHorario == null)
        {
            return NotFound(); // No existe
        }

        // Verificar que el horario pertenece al usuario
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
            return NotFound(); // No pertenece al usuario
        }

        // Validar que se seleccione al menos una (Clase o Actividad)
        if (!horarioModel.ClaseId.HasValue && !horarioModel.ActividadId.HasValue)
        {
            ModelState.AddModelError("", "Debes seleccionar una Clase o una Actividad.");
        }

        // Validar que no se seleccionen ambas
        if (horarioModel.ClaseId.HasValue && horarioModel.ActividadId.HasValue)
        {
            ModelState.AddModelError("", "Debes seleccionar solo una Clase o una Actividad, no ambas.");
        }

        // Validar que la nueva ClaseId también pertenezca al usuario (evitar mover horarios a clases de otros)
        if (horarioModel.ClaseId.HasValue)
        {
            var newClase = await _context.Clases.FindAsync(horarioModel.ClaseId.Value);
            if (newClase == null || newClase.UsuarioId != userId)
            {
                ModelState.AddModelError("ClaseId", "La clase seleccionada no es válida o no te pertenece.");
            }
        }

        // Validar que la nueva ActividadId también pertenezca al usuario
        if (horarioModel.ActividadId.HasValue)
        {
            var newActividad = await _context.Actividades.FindAsync(horarioModel.ActividadId.Value);
            if (newActividad == null || newActividad.UsuarioId != userId)
            {
                ModelState.AddModelError("ActividadId", "La actividad seleccionada no es válida o no te pertenece.");
            }
        }

        // Validar conflictos de horario (excluyendo el horario actual)
        var hasConflict = await CheckScheduleConflict(
            userId, 
            horarioModel.DiaSemana, 
            horarioModel.HoraInicio, 
            horarioModel.HoraFin, 
            id // Excluir el horario actual de la validación
        );

        if (hasConflict)
        {
            ModelState.AddModelError("", "El rango de horas seleccionado ya está ocupado por otra actividad o clase en ese día.");
        }
        
        if (ModelState.IsValid)
        {
            try
            {
                // Aseguramos que solo se actualicen los campos relevantes
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

        // Recargar los dropdowns si hay errores de validación
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
    
    // ======================== ELIMINAR HORARIO (GET & POST) ========================
    // Usaremos un método simple para obtener el horario para eliminar
    public async Task<IActionResult> Eliminar(int? id)
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
            return NotFound(); // No existe
        }

        // Verificar que el horario pertenece al usuario
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
            return NotFound(); // No pertenece al usuario
        }

        return View(horarioModel);
    }
    
    [HttpPost, ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(int id)
    {
        int userId = GetCurrentUserIdInt();
        if (userId == 0) return Forbid();
        
        var horarioModel = await _context.Horarios
            .Include(h => h.Clase)
            .Include(h => h.Actividad)
            .FirstOrDefaultAsync(m => m.Id == id);

        // Doble chequeo de seguridad
        if (horarioModel == null)
        {
             TempData["ErrorMessage"] = "No se pudo encontrar este Horario.";
             return RedirectToAction(nameof(Index));
        }

        // Verificar que el horario pertenece al usuario
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


    // El resto de métodos no son estrictamente necesarios para el CRUD, pero los dejamos como base
    private bool HorarioModelExists(int id)
    {
        return _context.Horarios.Any(e => e.Id == id);
    }

    // Mantener o eliminar el método Details según si lo usas
    // Lo simplificaré para que también aplique la seguridad
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

        // Verificar que el horario pertenece al usuario
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