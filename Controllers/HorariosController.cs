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

        private int GetCurrentUserIdInt()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdString, out int id) ? id : 0;
        }

        // GET: Horarios
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            var horarios = await _context.Horarios
                .Include(h => h.Clase)
                .Include(h => h.Actividad)
                .Where(h => (h.Clase != null && h.Clase.UsuarioId == userId) ||
                           (h.Actividad != null && h.Actividad.UsuarioId == userId))
                .ToListAsync();

            return View(horarios);
        }

        // GET: Horarios/Create
        public async Task<IActionResult> Create()
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            ViewData["ClaseId"] = new SelectList(
                await _context.Clases.Where(c => c.UsuarioId == userId).ToListAsync(),
                "Id", "Nombre");

            ViewData["ActividadId"] = new SelectList(
                await _context.Actividades.Where(a => a.UsuarioId == userId).ToListAsync(),
                "Id", "Nombre");

            return View();
        }

        // POST: Horarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClaseId,ActividadId,DiaSemana,HoraInicio,HoraFin,EsRecurrente,FechaEspecifica")] HorarioModel horario)
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            // Si NO es recurrente, extraer el día de la semana de la fecha específica
            if (!horario.EsRecurrente && horario.FechaEspecifica.HasValue)
            {
                // ⭐ CONVERTIR A UTC PARA POSTGRESQL
                horario.FechaEspecifica = DateTime.SpecifyKind(horario.FechaEspecifica.Value, DateTimeKind.Utc);

                var cultura = new CultureInfo("es-ES");
                horario.DiaSemana = cultura.DateTimeFormat.GetDayName(horario.FechaEspecifica.Value.DayOfWeek);
                horario.DiaSemana = char.ToUpper(horario.DiaSemana[0]) + horario.DiaSemana.Substring(1);
            }

            // Si es recurrente, limpiar la fecha específica
            if (horario.EsRecurrente)
            {
                horario.FechaEspecifica = null;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(horario);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Horario creado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al guardar: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                    }
                    ModelState.AddModelError("", $"Error al guardar: {ex.InnerException?.Message ?? ex.Message}");
                }
            }

            // Si hay errores, recargar los dropdowns
            ViewData["ClaseId"] = new SelectList(
                await _context.Clases.Where(c => c.UsuarioId == userId).ToListAsync(),
                "Id", "Nombre", horario.ClaseId);

            ViewData["ActividadId"] = new SelectList(
                await _context.Actividades.Where(a => a.UsuarioId == userId).ToListAsync(),
                "Id", "Nombre", horario.ActividadId);

            return View(horario);
        }

        // GET: Horarios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            var horario = await _context.Horarios
                .Include(h => h.Clase)
                .Include(h => h.Actividad)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (horario == null) return NotFound();

            if ((horario.ClaseId.HasValue && horario.Clase?.UsuarioId != userId) ||
                (horario.ActividadId.HasValue && horario.Actividad?.UsuarioId != userId))
            {
                return Forbid();
            }

            ViewData["ClaseId"] = new SelectList(
                await _context.Clases.Where(c => c.UsuarioId == userId).ToListAsync(),
                "Id", "Nombre", horario.ClaseId);

            ViewData["ActividadId"] = new SelectList(
                await _context.Actividades.Where(a => a.UsuarioId == userId).ToListAsync(),
                "Id", "Nombre", horario.ActividadId);

            return View(horario);
        }

        // POST: Horarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClaseId,ActividadId,DiaSemana,HoraInicio,HoraFin,EsRecurrente,FechaEspecifica")] HorarioModel horario)
        {
            if (id != horario.Id) return NotFound();

            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            // Si NO es recurrente, extraer el día de la semana
            if (!horario.EsRecurrente && horario.FechaEspecifica.HasValue)
            {
                // ⭐ CONVERTIR A UTC PARA POSTGRESQL
                horario.FechaEspecifica = DateTime.SpecifyKind(horario.FechaEspecifica.Value, DateTimeKind.Utc);

                var cultura = new CultureInfo("es-ES");
                horario.DiaSemana = cultura.DateTimeFormat.GetDayName(horario.FechaEspecifica.Value.DayOfWeek);
                horario.DiaSemana = char.ToUpper(horario.DiaSemana[0]) + horario.DiaSemana.Substring(1);
            }

            // Si es recurrente, limpiar la fecha específica
            if (horario.EsRecurrente)
            {
                horario.FechaEspecifica = null;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(horario);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Horario actualizado exitosamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Horarios.Any(e => e.Id == horario.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["ClaseId"] = new SelectList(
                await _context.Clases.Where(c => c.UsuarioId == userId).ToListAsync(),
                "Id", "Nombre", horario.ClaseId);

            ViewData["ActividadId"] = new SelectList(
                await _context.Actividades.Where(a => a.UsuarioId == userId).ToListAsync(),
                "Id", "Nombre", horario.ActividadId);

            return View(horario);
        }

        // GET: Horarios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            var horario = await _context.Horarios
                .Include(h => h.Clase)
                .Include(h => h.Actividad)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (horario == null) return NotFound();

            if ((horario.ClaseId.HasValue && horario.Clase?.UsuarioId != userId) ||
                (horario.ActividadId.HasValue && horario.Actividad?.UsuarioId != userId))
            {
                return Forbid();
            }

            return View(horario);
        }

        // GET: Horarios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            var horario = await _context.Horarios
                .Include(h => h.Clase)
                .Include(h => h.Actividad)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (horario == null) return NotFound();

            if ((horario.ClaseId.HasValue && horario.Clase?.UsuarioId != userId) ||
                (horario.ActividadId.HasValue && horario.Actividad?.UsuarioId != userId))
            {
                return Forbid();
            }

            return View(horario);
        }

        // POST: Horarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            var horario = await _context.Horarios
                .Include(h => h.Clase)
                .Include(h => h.Actividad)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (horario != null)
            {
                if ((horario.ClaseId.HasValue && horario.Clase?.UsuarioId == userId) ||
                    (horario.ActividadId.HasValue && horario.Actividad?.UsuarioId == userId))
                {
                    _context.Horarios.Remove(horario);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Horario eliminado exitosamente.";
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}