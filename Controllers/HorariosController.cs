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

        // 2. Obtener los horarios cuyas Clases pertenecen al usuario actual
        var horarios = await _context.Horarios
            .Include(h => h.Clase) // Incluir el objeto Clase para mostrar su nombre
            .Where(h => h.ClaseId.HasValue && userClasesIds.Contains(h.ClaseId.Value))
            .ToListAsync();

        // Si no hay clases registradas para el usuario, tampoco hay horarios.
        if (!userClasesIds.Any()) 
        {
            TempData["InfoMessage"] = "Aún no tienes clases registradas, por favor registra una para agregarle horarios.";
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

        if (!userClases.Any())
        {
            TempData["ErrorMessage"] = "Debes crear una Clase primero para poder asignarle un Horario.";
            return RedirectToAction(nameof(Index), "Clases"); // Redirigir al listado de Clases
        }

        ViewData["ClaseId"] = new SelectList(userClases, "Id", "Nombre");
        return View();
    }

    // ======================== CREAR HORARIO (POST) ========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ClaseId,DiaSemana,HoraInicio,HoraFin")] HorarioModel horarioModel)
    {
        int userId = GetCurrentUserIdInt();
        if (userId == 0) return Forbid();

        // Validar si la ClaseId enviada realmente pertenece al usuario actual (Seguridad)
        var clase = await _context.Clases.FindAsync(horarioModel.ClaseId);
        if (clase == null || clase.UsuarioId != userId)
        {
            return Forbid(); // Acceso no autorizado o clase inexistente
        }

        if (ModelState.IsValid)
        {
            _context.Add(horarioModel);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Horario creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // Recargar el dropdown si hay errores de validación
        var userClases = await _context.Clases
            .Where(c => c.UsuarioId == userId)
            .ToListAsync();

        ViewData["ClaseId"] = new SelectList(userClases, "Id", "Nombre", horarioModel.ClaseId);
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
            .FirstOrDefaultAsync(h => h.Id == id);

        if (horarioModel == null || horarioModel.Clase?.UsuarioId != userId)
        {
            return NotFound(); // No existe o no pertenece al usuario
        }

        var userClases = await _context.Clases
            .Where(c => c.UsuarioId == userId)
            .ToListAsync();

        ViewData["ClaseId"] = new SelectList(userClases, "Id", "Nombre", horarioModel.ClaseId);
        return View(horarioModel);
    }

    // ======================== MODIFICAR HORARIO (POST) ========================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ClaseId,DiaSemana,HoraInicio,HoraFin")] HorarioModel horarioModel)
    {
        if (id != horarioModel.Id) return NotFound();

        int userId = GetCurrentUserIdInt();
        if (userId == 0) return Forbid();

        // Validar si el horario a modificar realmente pertenece a una clase del usuario actual
        var existingHorario = await _context.Horarios
            .Include(h => h.Clase)
            .AsNoTracking() // Importante para que EF no intente trackear dos entidades con el mismo ID
            .FirstOrDefaultAsync(h => h.Id == id);

        if (existingHorario == null || existingHorario.Clase?.UsuarioId != userId)
        {
            return NotFound(); // No existe o no pertenece al usuario
        }

        // Validar que la nueva ClaseId también pertenezca al usuario (evitar mover horarios a clases de otros)
        var newClase = await _context.Clases.FindAsync(horarioModel.ClaseId);
        if (newClase == null || newClase.UsuarioId != userId)
        {
            ModelState.AddModelError("ClaseId", "La clase seleccionada no es válida o no te pertenece.");
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

        // Recargar el dropdown si hay errores de validación
        var userClases = await _context.Clases
            .Where(c => c.UsuarioId == userId)
            .ToListAsync();
        ViewData["ClaseId"] = new SelectList(userClases, "Id", "Nombre", horarioModel.ClaseId);
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
            .FirstOrDefaultAsync(m => m.Id == id);
            
        if (horarioModel == null || horarioModel.Clase?.UsuarioId != userId)
        {
            return NotFound(); // No existe o no pertenece al usuario
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
            .FirstOrDefaultAsync(m => m.Id == id);

        // Doble chequeo de seguridad
        if (horarioModel == null || horarioModel.Clase?.UsuarioId != userId)
        {
             TempData["ErrorMessage"] = "No se pudo encontrar o no tienes permiso para eliminar este Horario.";
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
            .FirstOrDefaultAsync(m => m.Id == id);

        if (horarioModel == null || horarioModel.Clase?.UsuarioId != userId)
        {
            return NotFound();
        }

        return View(horarioModel);
    }
}