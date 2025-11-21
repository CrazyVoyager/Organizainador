// ActividadesController.cs (VERSION CORREGIDA Y SEGURA)
using System;
using System.Linq;
using System.Threading.Tasks;
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

        public ActividadesController(AppDbContext context)
        {
            _context = context;
        }

        // Funciones Auxiliares para obtener el ID del usuario logueado.
        private string GetCurrentUserIdString() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        private int GetCurrentUserIdInt() => int.TryParse(GetCurrentUserIdString(), out int id) ? id : 0;

        // ======================== LISTADO PRINCIPAL (Index) ========================
        // GET: Actividades
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            // 1. Filtrar SOLO las actividades del usuario logueado e incluir horarios
            var actividades = await _context.Actividades
                                            .Include(a => a.Horarios)
                                            .Where(a => a.UsuarioId == userId)
                                            .ToListAsync();
            return View(actividades);
        }

        // ======================== DETALLES (Details) ========================
        // GET: Actividades/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0 || id == null) return NotFound();

            var actividadModel = await _context.Actividades
                .Include(a => a.Horarios)
                .FirstOrDefaultAsync(m => m.Id == id && m.UsuarioId == userId);

            if (actividadModel == null) return NotFound();

            return View(actividadModel);
        }

        // ======================== CREAR ACTIVIDAD (Create) ========================
        // GET: Actividades/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Actividades/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Nombre,Descripcion,Etiqueta,FechaInicio,FechaFin")] ActividadModel actividadModel)
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            // 1. ASIGNAR EL ID del usuario logueado
            actividadModel.UsuarioId = userId;
            actividadModel.CreatedAt = DateTime.UtcNow; // Asignar la fecha de creación automáticamente

            if (ModelState.IsValid)
            {
                _context.Actividades.Add(actividadModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(actividadModel);
        }

        // ======================== MODIFICAR ACTIVIDAD (Edit) ========================
        // GET: Actividades/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0 || id == null) return NotFound();

            var actividadModel = await _context.Actividades
                .Include(a => a.Horarios)
                .FirstOrDefaultAsync(a => a.Id == id);

            // 1. Validar que la actividad existe Y pertenece al usuario logueado
            if (actividadModel == null || actividadModel.UsuarioId != userId) return NotFound();

            return View(actividadModel);
        }

        // POST: Actividades/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,Nombre,Descripcion,Etiqueta,FechaInicio,FechaFin")] ActividadModel actividadModel)
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            if (id != actividadModel.Id) return NotFound();

            // 1. Re-asignar la FK y CreatedAt para seguridad
            actividadModel.UsuarioId = userId;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(actividadModel);
                    // 2. Marcar estas propiedades como NO modificadas
                    _context.Entry(actividadModel).Property(a => a.UsuarioId).IsModified = false;
                    _context.Entry(actividadModel).Property(a => a.CreatedAt).IsModified = false;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Actividades.Any(e => e.Id == actividadModel.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(actividadModel);
        }

        // ======================== ELIMINAR ACTIVIDAD (Delete) ========================
        // GET: Actividades/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0 || id == null) return NotFound();

            var actividadModel = await _context.Actividades
                .FirstOrDefaultAsync(m => m.Id == id && m.UsuarioId == userId);

            if (actividadModel == null) return NotFound();

            return View(actividadModel);
        }

        // POST: Actividades/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            var actividadModel = await _context.Actividades.FindAsync(id);

            if (actividadModel != null && actividadModel.UsuarioId == userId)
            {
                _context.Actividades.Remove(actividadModel);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ActividadModelExists(int id)
        {
            return _context.Actividades.Any(e => e.Id == id);
        }
    }
}