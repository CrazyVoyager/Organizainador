using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Organizainador.Data;
using Organizainador.Models;
using System.Security.Claims;

namespace Organizainador.Controllers
{
    public class ClasesController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ClasesController> _logger;

        public ClasesController(AppDbContext context, ILogger<ClasesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ======================== LISTADO PRINCIPAL ========================
        [HttpGet]
        public async Task<IActionResult> Index(string? busqueda)
        {
            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                var query = _context.Clases
                    .Include(c => c.Horarios) // ⭐ AGREGAR ESTA LÍNEA
                    .Where(c => c.UsuarioId == userId)
                    .AsQueryable();

                // Búsqueda - EF Core previene SQL injection automáticamente al usar LINQ
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    // Sanitizar entrada para prevenir ataques
                    busqueda = busqueda.Trim();
                    query = query.Where(c =>
                        c.Nombre.Contains(busqueda) ||
                        (c.Descripcion != null && c.Descripcion.Contains(busqueda))
                    );
                    ViewData["BusquedaActual"] = busqueda;
                }

                var clases = await query
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();

                return View(clases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la lista de clases");
                TempData["ErrorMessage"] = "Ocurrió un error al cargar las clases.";
                return View(new List<ClaseModel>());
            }
        }

        // ======================== DETALLE DE CLASE ========================
        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                var clase = await _context.Clases
                    .FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == userId);

                if (clase == null)
                {
                    TempData["ErrorMessage"] = "Clase no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                // Obtener horarios relacionados
                var horarios = await _context.Horarios
                    .Where(h => h.ClaseId == id)
                    .OrderBy(h => h.DiaSemana)
                    .ThenBy(h => h.HoraInicio)
                    .ToListAsync();

                ViewData["Horarios"] = horarios;

                return View(clase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el detalle de la clase {Id}", id);
                TempData["ErrorMessage"] = "Error al cargar el detalle de la clase.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ======================== CREAR CLASE ========================
        [HttpGet]
        public IActionResult Crear()
        {
            return View(new ClaseModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ClaseModel clase)
        {
            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                // Asignar usuario
                clase.UsuarioId = userId;

                // Remover validación de navegación si existe
                ModelState.Remove("Horarios");

                if (!ModelState.IsValid)
                {
                    // Log de errores de validación
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning("Error de validación: {ErrorMessage}", error.ErrorMessage);
                    }
                    return View(clase);
                }

                // Asegurarse de que la colección de Horarios sea null al crear
                clase.Horarios = null;

                _context.Clases.Add(clase);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Clase creada: {Nombre} por usuario {UserId}", clase.Nombre, userId);
                SetSuccessMessage($"Clase '{clase.Nombre}' creada exitosamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Error de base de datos al crear clase: {InnerException}", dbEx.InnerException?.Message);
                ModelState.AddModelError("", $"Error de base de datos: {dbEx.InnerException?.Message ?? dbEx.Message}");
                return View(clase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear clase: {Message}", ex.Message);
                ModelState.AddModelError("", $"Ocurrió un error al crear la clase: {ex.Message}");
                return View(clase);
            }
        }

        // ======================== EDITAR CLASE ========================
        [HttpGet]
        public async Task<IActionResult> Modificar(int id)
        {
            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                var clase = await _context.Clases
                    .FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == userId);

                if (clase == null)
                {
                    TempData["ErrorMessage"] = "Clase no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                return View(clase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la clase {Id} para editar", id);
                TempData["ErrorMessage"] = "Error al cargar la clase.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Modificar(int id, ClaseModel clase)
        {
            if (id != clase.Id)
            {
                return NotFound();
            }

            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                // Verificar pertenencia
                var claseExistente = await _context.Clases
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == userId);

                if (claseExistente == null)
                {
                    TempData["ErrorMessage"] = "Clase no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                // Preservar usuario
                clase.UsuarioId = userId;

                if (!ModelState.IsValid)
                {
                    return View(clase);
                }

                _context.Update(clase);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Clase actualizada: {Id} - {Nombre}", clase.Id, clase.Nombre);
                SetSuccessMessage($"Clase '{clase.Nombre}' actualizada exitosamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ClaseExistsAsync(clase.Id))
                {
                    TempData["ErrorMessage"] = "La clase ya no existe.";
                    return RedirectToAction(nameof(Index));
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar clase {Id}", id);
                ModelState.AddModelError("", "Ocurrió un error al actualizar la clase.");
                return View(clase);
            }
        }

        // ======================== ELIMINAR CLASE ========================
        [HttpGet]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                var clase = await _context.Clases
                    .FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == userId);

                if (clase == null)
                {
                    TempData["ErrorMessage"] = "Clase no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar horarios relacionados
                var tieneHorarios = await _context.Horarios.AnyAsync(h => h.ClaseId == id);
                ViewData["TieneHorarios"] = tieneHorarios;
                ViewData["PuedeEliminar"] = !tieneHorarios;

                return View(clase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la clase {Id} para eliminar", id);
                TempData["ErrorMessage"] = "Error al cargar la clase.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            try
            {
                int userId = GetCurrentUserIdInt();
                if (userId == 0) return Forbid();

                var clase = await _context.Clases
                    .FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == userId);

                if (clase == null)
                {
                    TempData["ErrorMessage"] = "Clase no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar horarios relacionados
                var tieneHorarios = await _context.Horarios.AnyAsync(h => h.ClaseId == id);
                if (tieneHorarios)
                {
                    TempData["ErrorMessage"] = "No se puede eliminar la clase porque tiene horarios asociados.";
                    return RedirectToAction(nameof(Eliminar), new { id });
                }

                _context.Clases.Remove(clase);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Clase eliminada: {Id} - {Nombre}", clase.Id, clase.Nombre);
                SetSuccessMessage($"Clase '{clase.Nombre}' eliminada exitosamente.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar clase {Id}", id);
                TempData["ErrorMessage"] = "Error al eliminar la clase.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ======================== MÉTODOS AUXILIARES ========================
        private async Task<bool> ClaseExistsAsync(int id)
        {
            return await _context.Clases.AnyAsync(e => e.Id == id);
        }
    }
}