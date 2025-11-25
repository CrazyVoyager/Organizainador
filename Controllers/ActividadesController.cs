using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Organizainador.Data;
using Organizainador.Models;
using System.Security.Claims;

namespace Organizainador.Controllers
{
    public class ActividadesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ActividadesController> _logger;

        public ActividadesController(AppDbContext context, ILogger<ActividadesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ======================== MÉTODOS AUXILIARES ========================
        private string GetCurrentUserIdString() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        private int GetCurrentUserIdInt() => int.TryParse(GetCurrentUserIdString(), out int id) ? id : 0;

        // ======================== LISTADO PRINCIPAL ========================
        [HttpGet]
        public async Task<IActionResult> Index(string? busqueda, string? filtroEtiqueta)
        {
            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                var query = _context.Actividades
                    .Where(a => a.UsuarioId == userId)
                    .AsQueryable();

                // Búsqueda
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    query = query.Where(a =>
                        a.Nombre.Contains(busqueda) ||
                        (a.Descripcion != null && a.Descripcion.Contains(busqueda))
                    );
                    ViewData["BusquedaActual"] = busqueda;
                }

                // Filtro por etiqueta
                if (!string.IsNullOrWhiteSpace(filtroEtiqueta))
                {
                    query = query.Where(a => a.Etiqueta == filtroEtiqueta);
                    ViewData["FiltroEtiquetaActual"] = filtroEtiqueta;
                }

                // Obtener etiquetas únicas para el filtro
                var etiquetas = await _context.Actividades
                    .Where(a => a.UsuarioId == userId && a.Etiqueta != null)
                    .Select(a => a.Etiqueta)
                    .Distinct()
                    .OrderBy(e => e)
                    .ToListAsync();

                ViewData["Etiquetas"] = etiquetas;

                var actividades = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                return View(actividades);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la lista de actividades");
                TempData["ErrorMessage"] = "Ocurrió un error al cargar las actividades.";
                return View(new List<ActividadModel>());
            }
        }

        // ======================== DETALLE DE ACTIVIDAD ========================
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                var actividad = await _context.Actividades
                    .FirstOrDefaultAsync(a => a.Id == id && a.UsuarioId == userId);

                if (actividad == null)
                {
                    TempData["ErrorMessage"] = "Actividad no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                // Obtener horarios relacionados
                var horarios = await _context.Horarios
                    .Where(h => h.ActividadId == id)
                    .OrderBy(h => h.DiaSemana)
                    .ThenBy(h => h.HoraInicio)
                    .ToListAsync();

                ViewData["Horarios"] = horarios;

                return View(actividad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el detalle de la actividad {Id}", id);
                TempData["ErrorMessage"] = "Error al cargar el detalle de la actividad.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ======================== CREAR ACTIVIDAD ========================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new ActividadModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActividadModel actividad)
        {
            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                // Asignar usuario y fecha
                actividad.UsuarioId = userId;
                actividad.CreatedAt = DateTime.UtcNow;

                if (!ModelState.IsValid)
                {
                    return View(actividad);
                }

                _context.Actividades.Add(actividad);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Actividad creada: {Nombre} por usuario {UserId}", actividad.Nombre, userId);
                TempData["SuccessMessage"] = $"Actividad '{actividad.Nombre}' creada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear actividad");
                ModelState.AddModelError("", "Ocurrió un error al crear la actividad. Por favor, intenta nuevamente.");
                return View(actividad);
            }
        }

        // ======================== EDITAR ACTIVIDAD ========================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                var actividad = await _context.Actividades
                    .FirstOrDefaultAsync(a => a.Id == id && a.UsuarioId == userId);

                if (actividad == null)
                {
                    TempData["ErrorMessage"] = "Actividad no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                return View(actividad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la actividad {Id} para editar", id);
                TempData["ErrorMessage"] = "Error al cargar la actividad.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ActividadModel actividad)
        {
            if (id != actividad.Id)
            {
                return NotFound();
            }

            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                // Verificar pertenencia
                var actividadExistente = await _context.Actividades
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == id && a.UsuarioId == userId);

                if (actividadExistente == null)
                {
                    TempData["ErrorMessage"] = "Actividad no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                // Preservar valores originales
                actividad.UsuarioId = userId;
                actividad.CreatedAt = actividadExistente.CreatedAt;

                if (!ModelState.IsValid)
                {
                    return View(actividad);
                }

                _context.Update(actividad);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Actividad actualizada: {Id} - {Nombre}", actividad.Id, actividad.Nombre);
                TempData["SuccessMessage"] = $"Actividad '{actividad.Nombre}' actualizada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ActividadExistsAsync(actividad.Id))
                {
                    TempData["ErrorMessage"] = "La actividad ya no existe.";
                    return RedirectToAction(nameof(Index));
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar actividad {Id}", id);
                ModelState.AddModelError("", "Ocurrió un error al actualizar la actividad.");
                return View(actividad);
            }
        }

        // ======================== ELIMINAR ACTIVIDAD ========================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                var actividad = await _context.Actividades
                    .FirstOrDefaultAsync(a => a.Id == id && a.UsuarioId == userId);

                if (actividad == null)
                {
                    TempData["ErrorMessage"] = "Actividad no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar horarios relacionados
                var tieneHorarios = await _context.Horarios.AnyAsync(h => h.ActividadId == id);
                ViewData["TieneHorarios"] = tieneHorarios;
                ViewData["PuedeEliminar"] = !tieneHorarios;

                return View(actividad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la actividad {Id} para eliminar", id);
                TempData["ErrorMessage"] = "Error al cargar la actividad.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                var actividad = await _context.Actividades
                    .FirstOrDefaultAsync(a => a.Id == id && a.UsuarioId == userId);

                if (actividad == null)
                {
                    TempData["ErrorMessage"] = "Actividad no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar horarios relacionados
                var tieneHorarios = await _context.Horarios.AnyAsync(h => h.ActividadId == id);
                if (tieneHorarios)
                {
                    TempData["ErrorMessage"] = "No se puede eliminar la actividad porque tiene horarios asociados.";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                _context.Actividades.Remove(actividad);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Actividad eliminada: {Id} - {Nombre}", actividad.Id, actividad.Nombre);
                TempData["SuccessMessage"] = $"Actividad '{actividad.Nombre}' eliminada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar actividad {Id}", id);
                TempData["ErrorMessage"] = "Error al eliminar la actividad.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ======================== MÉTODOS AUXILIARES ========================
        private async Task<bool> ActividadExistsAsync(int id)
        {
            return await _context.Actividades.AnyAsync(e => e.Id == id);
        }
    }
}