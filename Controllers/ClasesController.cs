// ClasesController.cs (VERSION CORREGIDA Y SEGURA)
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Organizainador.Models;
using Organizainador.Data;
using System.Linq;
using System.Security.Claims; // Necesario para FindFirstValue
using System.Threading.Tasks;

namespace Organizainador.Controllers
{
    public class ClasesController : Controller
    {
        private readonly AppDbContext _context;

        public ClasesController(AppDbContext context)
        {
            _context = context;
        }

        // Función Auxiliar para obtener el ID del usuario logueado.
        // Asume que tu UsuarioId en ClaseModel es INT, pero Identity lo maneja como STRING.
        private string GetCurrentUserIdString() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        private int GetCurrentUserIdInt() => int.TryParse(GetCurrentUserIdString(), out int id) ? id : 0;

        // ======================== LISTADO PRINCIPAL (Index) ========================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid(); // Redirigir si no está logueado o el ID no es válido

            // 1. Filtrar SOLO las clases del usuario logueado
            var clases = await _context.Clases
                                       .Where(c => c.UsuarioId == userId)
                                       .ToListAsync();

            // Ya no es necesario cargar ViewBag.Usuarios
            return View(clases);
        }

        // ======================== CREAR CLASE (Crear) ========================
        [HttpGet]
        public IActionResult Crear()
        {
            // Ya no necesitas cargar el ViewBag.Usuarios
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
            [Bind("Nombre,Descripcion,CantidadHorasDia")] ClaseModel clase)
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            // 1. ASIGNAR EL ID del usuario logueado (Ignorando lo que venga del formulario)
            clase.UsuarioId = userId;

            if (ModelState.IsValid)
            {
                _context.Clases.Add(clase);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            // Si la validación falla, regresa la vista. Ya no necesita el ViewBag.
            return View(clase);
        }

        // ======================== MODIFICAR CLASE (Modificar) ========================
        [HttpGet]
        public async Task<IActionResult> Modificar(int id)
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            var clase = await _context.Clases.FindAsync(id);

            // 1. Validar que la clase existe Y pertenece al usuario logueado
            if (clase == null || clase.UsuarioId != userId) return NotFound();

            // Ya no es necesario cargar el ViewBag.Usuarios
            return View(clase);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Modificar(int id, [Bind("Id,Nombre,Descripcion,CantidadHorasDia")] ClaseModel clase)
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            if (id != clase.Id) return NotFound();

            // 1. Re-asignar la FK de seguridad. Esto previene que un atacante cambie el UsuarioId.
            clase.UsuarioId = userId;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(clase);
                    // 2. Marcar la propiedad UsuarioId como NO modificada, por si acaso
                    _context.Entry(clase).Property(c => c.UsuarioId).IsModified = false;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Clases.Any(e => e.Id == clase.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction("Index");
            }

            // Si falla la validación, regresa la vista.
            return View(clase);
        }

        // ======================== ELIMINAR CLASE (Eliminar) ========================
        [HttpGet]
        public async Task<IActionResult> Eliminar(int id)
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            var clase = await _context.Clases
                .FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == userId); // Filtrar por ID de clase Y de usuario

            if (clase == null) return NotFound();

            // Opcional: Si quieres mostrar el nombre del usuario en la vista de eliminación
            var usuario = await _context.Usuarios.FindAsync(clase.UsuarioId);
            ViewBag.UsuarioNombre = usuario?.Nombre; // Asume que Tab_usr es tu DbSet<UsuarioModel>

            return View(clase);
        }

        [HttpPost]
        [ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            int userId = GetCurrentUserIdInt();
            if (userId == 0) return Forbid();

            var clase = await _context.Clases.FindAsync(id);

            // Validar existencia y propiedad antes de eliminar
            if (clase != null && clase.UsuarioId == userId)
            {
                _context.Clases.Remove(clase);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Clase eliminada exitosamente";
            }
            return RedirectToAction("Index");
        }
    }
}