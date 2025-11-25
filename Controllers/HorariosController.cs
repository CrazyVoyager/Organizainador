using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Organizainador.Data;
using Organizainador.Models;
using System.Globalization;
using System.Security.Claims;

namespace Organizainador.Controllers
{
    public class HorariosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HorariosController> _logger;

        public HorariosController(AppDbContext context, ILogger<HorariosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ======================== MÉTODOS AUXILIARES ========================
        private string GetCurrentUserIdString() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        private int GetCurrentUserIdInt() => int.TryParse(GetCurrentUserIdString(), out int id) ? id : 0;

        // ======================== LISTADO PRINCIPAL ========================
        [HttpGet]
        public async Task<IActionResult> Index(string? busqueda, string? filtroTipo)
        {
            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                var query = _context.Horarios
                    .Include(h => h.Clase)
                    .Include(h => h.Actividad)
                    .Where(h => (h.Clase != null && h.Clase.UsuarioId == userId) ||
                               (h.Actividad != null && h.Actividad.UsuarioId == userId))
                    .AsQueryable();

                // Filtro por búsqueda
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    query = query.Where(h =>
                        (h.Clase != null && h.Clase.Nombre.Contains(busqueda)) ||
                        (h.Actividad != null && h.Actividad.Nombre.Contains(busqueda)) ||
                        (h.DiaSemana != null && h.DiaSemana.Contains(busqueda))
                    );
                    ViewData["BusquedaActual"] = busqueda;
                }

                // Filtro por tipo (Recurrente/Único)
                if (!string.IsNullOrWhiteSpace(filtroTipo))
                {
                    if (filtroTipo == "recurrente")
                        query = query.Where(h => h.EsRecurrente);
                    else if (filtroTipo == "unico")
                        query = query.Where(h => !h.EsRecurrente);
                    
                    ViewData["FiltroTipo"] = filtroTipo;
                }

                var horarios = await query
                    .OrderBy(h => h.EsRecurrente ? 0 : 1)
                    .ThenBy(h => h.DiaSemana)
                    .ThenBy(h => h.HoraInicio)
                    .ToListAsync();

                return View(horarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la lista de horarios");
                TempData["ErrorMessage"] = "Ocurrió un error al cargar los horarios.";
                return View(new List<HorarioModel>());
            }
        }

        // ======================== DETALLE DE HORARIO ========================
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                var horario = await _context.Horarios
                    .Include(h => h.Clase)
                    .Include(h => h.Actividad)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (horario == null)
                {
                    TempData["ErrorMessage"] = "Horario no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if ((horario.ClaseId.HasValue && horario.Clase?.UsuarioId != userId) ||
                    (horario.ActividadId.HasValue && horario.Actividad?.UsuarioId != userId))
                {
                    return Forbid();
                }

                return View(horario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el detalle del horario {Id}", id);
                TempData["ErrorMessage"] = "Error al cargar el detalle del horario.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ======================== CREAR HORARIO ========================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                await CargarSelectLists(userId);
                return View(new HorarioModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el formulario de creación");
                TempData["ErrorMessage"] = "Error al cargar el formulario.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClaseId,ActividadId,DiaSemana,HoraInicio,HoraFin,EsRecurrente,FechaEspecifica")] HorarioModel horario)
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            try
            {
                // Validar que pertenezca al usuario
                if (horario.ClaseId.HasValue)
                {
                    var clase = await _context.Clases.FindAsync(horario.ClaseId.Value);
                    if (clase == null || clase.UsuarioId != userId)
                    {
                        ModelState.AddModelError("", "La clase seleccionada no es válida.");
                        await CargarSelectLists(userId, horario);
                        return View(horario);
                    }
                }

                if (horario.ActividadId.HasValue)
                {
                    var actividad = await _context.Actividades.FindAsync(horario.ActividadId.Value);
                    if (actividad == null || actividad.UsuarioId != userId)
                    {
                        ModelState.AddModelError("", "La actividad seleccionada no es válida.");
                        await CargarSelectLists(userId, horario);
                        return View(horario);
                    }
                }

                // Si NO es recurrente, procesar la fecha específica
                if (!horario.EsRecurrente && horario.FechaEspecifica.HasValue)
                {
                    // ⭐ CORRECCIÓN: Mantener solo la fecha sin hora y convertir a UTC sin cambiar el día
                    var fechaSoloDate = horario.FechaEspecifica.Value.Date;
                    horario.FechaEspecifica = DateTime.SpecifyKind(fechaSoloDate, DateTimeKind.Utc);
                    
                    var cultura = new CultureInfo("es-ES");
                    horario.DiaSemana = cultura.DateTimeFormat.GetDayName(horario.FechaEspecifica.Value.DayOfWeek);
                    horario.DiaSemana = char.ToUpper(horario.DiaSemana[0]) + horario.DiaSemana.Substring(1);
                }

                // Si es recurrente, limpiar la fecha específica
                if (horario.EsRecurrente)
                {
                    horario.FechaEspecifica = null;
                }

                // Validar el modelo manualmente para obtener errores personalizados
                if (!TryValidateModel(horario))
                {
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning("Error de validación: {ErrorMessage}", error.ErrorMessage);
                    }
                    await CargarSelectLists(userId, horario);
                    return View(horario);
                }

                _context.Add(horario);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Horario creado: {Id} por usuario {UserId}", horario.Id, userId);
                TempData["SuccessMessage"] = "Horario creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Error de base de datos al crear horario: {InnerException}", dbEx.InnerException?.Message);
                ModelState.AddModelError("", $"Error de base de datos: {dbEx.InnerException?.Message ?? dbEx.Message}");
                await CargarSelectLists(userId, horario);
                return View(horario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear horario: {Message}", ex.Message);
                ModelState.AddModelError("", $"Ocurrió un error al crear el horario: {ex.Message}");
                await CargarSelectLists(userId, horario);
                return View(horario);
            }
        }

        // ======================== EDITAR HORARIO ========================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                var horario = await _context.Horarios
                    .Include(h => h.Clase)
                    .Include(h => h.Actividad)
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (horario == null)
                {
                    TempData["ErrorMessage"] = "Horario no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if ((horario.ClaseId.HasValue && horario.Clase?.UsuarioId != userId) ||
                    (horario.ActividadId.HasValue && horario.Actividad?.UsuarioId != userId))
                {
                    return Forbid();
                }

                await CargarSelectLists(userId, horario);
                return View(horario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el horario {Id} para editar", id);
                TempData["ErrorMessage"] = "Error al cargar el horario.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClaseId,ActividadId,DiaSemana,HoraInicio,HoraFin,EsRecurrente,FechaEspecifica")] HorarioModel horario)
        {
            if (id != horario.Id) return NotFound();

            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            try
            {
                // Verificar pertenencia
                var horarioExistente = await _context.Horarios
                    .Include(h => h.Clase)
                    .Include(h => h.Actividad)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (horarioExistente == null)
                {
                    TempData["ErrorMessage"] = "Horario no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if ((horarioExistente.ClaseId.HasValue && horarioExistente.Clase?.UsuarioId != userId) ||
                    (horarioExistente.ActividadId.HasValue && horarioExistente.Actividad?.UsuarioId != userId))
                {
                    return Forbid();
                }

                // Validar que pertenezca al usuario
                if (horario.ClaseId.HasValue)
                {
                    var clase = await _context.Clases.FindAsync(horario.ClaseId.Value);
                    if (clase == null || clase.UsuarioId != userId)
                    {
                        ModelState.AddModelError("", "La clase seleccionada no es válida.");
                        await CargarSelectLists(userId, horario);
                        return View(horario);
                    }
                }

                if (horario.ActividadId.HasValue)
                {
                    var actividad = await _context.Actividades.FindAsync(horario.ActividadId.Value);
                    if (actividad == null || actividad.UsuarioId != userId)
                    {
                        ModelState.AddModelError("", "La actividad seleccionada no es válida.");
                        await CargarSelectLists(userId, horario);
                        return View(horario);
                    }
                }

                // Si NO es recurrente, procesar la fecha específica
                if (!horario.EsRecurrente && horario.FechaEspecifica.HasValue)
                {
                    // ⭐ CORRECCIÓN: Mantener solo la fecha sin hora y convertir a UTC sin cambiar el día
                    var fechaSoloDate = horario.FechaEspecifica.Value.Date;
                    horario.FechaEspecifica = DateTime.SpecifyKind(fechaSoloDate, DateTimeKind.Utc);
                    
                    var cultura = new CultureInfo("es-ES");
                    horario.DiaSemana = cultura.DateTimeFormat.GetDayName(horario.FechaEspecifica.Value.DayOfWeek);
                    horario.DiaSemana = char.ToUpper(horario.DiaSemana[0]) + horario.DiaSemana.Substring(1);
                }

                // Si es recurrente, limpiar la fecha específica
                if (horario.EsRecurrente)
                {
                    horario.FechaEspecifica = null;
                }

                if (!TryValidateModel(horario))
                {
                    await CargarSelectLists(userId, horario);
                    return View(horario);
                }

                _context.Update(horario);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Horario actualizado: {Id}", horario.Id);
                TempData["SuccessMessage"] = "Horario actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await HorarioExistsAsync(horario.Id))
                {
                    TempData["ErrorMessage"] = "El horario ya no existe.";
                    return RedirectToAction(nameof(Index));
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar horario {Id}", id);
                ModelState.AddModelError("", "Ocurrió un error al actualizar el horario.");
                await CargarSelectLists(userId, horario);
                return View(horario);
            }
        }

        // ======================== ELIMINAR HORARIO ========================
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                var horario = await _context.Horarios
                    .Include(h => h.Clase)
                    .Include(h => h.Actividad)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (horario == null)
                {
                    TempData["ErrorMessage"] = "Horario no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if ((horario.ClaseId.HasValue && horario.Clase?.UsuarioId != userId) ||
                    (horario.ActividadId.HasValue && horario.Actividad?.UsuarioId != userId))
                {
                    return Forbid();
                }

                return View(horario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el horario {Id} para eliminar", id);
                TempData["ErrorMessage"] = "Error al cargar el horario.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                var horario = await _context.Horarios
                    .Include(h => h.Clase)
                    .Include(h => h.Actividad)
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (horario == null)
                {
                    TempData["ErrorMessage"] = "Horario no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                if ((horario.ClaseId.HasValue && horario.Clase?.UsuarioId != userId) ||
                    (horario.ActividadId.HasValue && horario.Actividad?.UsuarioId != userId))
                {
                    return Forbid();
                }

                _context.Horarios.Remove(horario);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Horario eliminado: {Id}", horario.Id);
                TempData["SuccessMessage"] = "Horario eliminado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar horario {Id}", id);
                TempData["ErrorMessage"] = "Error al eliminar el horario.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ======================== MÉTODOS AUXILIARES PRIVADOS ========================
        private async Task CargarSelectLists(int userId, HorarioModel? horario = null)
        {
            ViewData["ClaseId"] = new SelectList(
                await _context.Clases.Where(c => c.UsuarioId == userId).ToListAsync(),
                "Id", "Nombre", horario?.ClaseId);

            ViewData["ActividadId"] = new SelectList(
                await _context.Actividades.Where(a => a.UsuarioId == userId).ToListAsync(),
                "Id", "Nombre", horario?.ActividadId);
        }

        private async Task<bool> HorarioExistsAsync(int id)
        {
            return await _context.Horarios.AnyAsync(e => e.Id == id);
        }
    }
}