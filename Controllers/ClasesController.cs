using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Organizainador.Models;
using Organizainador.Data;

namespace Organizainador.Controllers
{
    public class ClasesController : Controller
    {
        private readonly AppDbContext _context;

        public ClasesController(AppDbContext context)
        {
            _context = context;
        }

        // ======================== LISTADO PRINCIPAL ========================
[HttpGet]
public async Task<IActionResult> Index()
{
    try
    {
        // Obtener usuarios y clases desde la base de datos
        var usuarios = await _context.Tab_usr.ToListAsync();
        
        // ✅ CORREGIDO: Quitar el .Include() incorrecto
        var clases = await _context.Tab_clas.ToListAsync();

        // ✅ DEBUG: Verificar datos
        Console.WriteLine($"=== DEBUG INDEX ===");
        Console.WriteLine($"Usuarios: {usuarios.Count}");
        Console.WriteLine($"Clases: {clases.Count}");
        
        foreach (var clase in clases)
        {
            Console.WriteLine($"Clase ID: {clase.Id}, Nombre: {clase.Nombre}, UsuarioId: {clase.UsuarioId}");
        }

        ViewBag.Usuarios = usuarios;
        return View(clases);
    }
    catch (Exception ex)
    {
        // ✅ DEBUG del error
        Console.WriteLine($"=== ERROR EN INDEX: {ex.Message} ===");
        Console.WriteLine($"Stack: {ex.StackTrace}");
        
        TempData["ErrorMessage"] = $"Error al cargar las clases: {ex.Message}";
        return View(new List<ClaseModel>());
    }
}
        // ======================== CREAR CLASE ========================
        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            ViewBag.Usuarios = await _context.Tab_usr.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ClaseModel clase)
        {
            try
            {
                Console.WriteLine("=== INICIANDO CREACIÓN DE CLASE ===");
                Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

                if (ModelState.IsValid)
                {
                    Console.WriteLine("Modelo válido, verificando usuario...");

                    // Verificar que el usuario existe
                    var usuario = await _context.Tab_usr.FindAsync(clase.UsuarioId);
                    if (usuario == null)
                    {
                        ModelState.AddModelError("UsuarioId", "El usuario seleccionado no existe");
                        ViewBag.Usuarios = await _context.Tab_usr.ToListAsync();
                        return View(clase);
                    }

                    Console.WriteLine($"Usuario encontrado: {usuario.Nombre}");

                    // Asignar valores por defecto si es necesario
                    if (clase.CantidadHorasDia <= 0)
                        clase.CantidadHorasDia = 1;

                    _context.Tab_clas.Add(clase);
                    await _context.SaveChangesAsync();
                    
                    Console.WriteLine("Clase guardada exitosamente en la BD");
                    
                    TempData["SuccessMessage"] = "Clase creada exitosamente";
                    return RedirectToAction("Index");
                }
                else
                {
                    Console.WriteLine("=== ERRORES DE VALIDACIÓN ===");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"Error: {error.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== EXCEPCIÓN: {ex.Message} ===");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                
                TempData["ErrorMessage"] = $"Error al crear la clase: {ex.Message}";
            }

            // Si llegamos aquí, algo salió mal
            ViewBag.Usuarios = await _context.Tab_usr.ToListAsync();
            return View(clase);
        }

        // Los métodos Modificar y Eliminar se mantienen igual...
        // ======================== MODIFICAR CLASE ========================
        [HttpGet]
        public async Task<IActionResult> Modificar(int id)
        {
            var clase = await _context.Tab_clas.FindAsync(id);
            if (clase == null) return NotFound();

            ViewBag.Usuarios = await _context.Tab_usr.ToListAsync();
            return View(clase);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Modificar(int id, ClaseModel clase)
        {
            if (id != clase.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Verificar que el usuario existe
                var usuario = await _context.Tab_usr.FindAsync(clase.UsuarioId);
                if (usuario == null)
                {
                    ModelState.AddModelError("UsuarioId", "El usuario seleccionado no existe");
                    ViewBag.Usuarios = await _context.Tab_usr.ToListAsync();
                    return View(clase);
                }

                try
                {
                    _context.Update(clase);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Clase modificada exitosamente";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClaseExists(clase.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index");
            }

            ViewBag.Usuarios = await _context.Tab_usr.ToListAsync();
            return View(clase);
        }

        // ======================== ELIMINAR CLASE ========================
        [HttpGet]
        public async Task<IActionResult> Eliminar(int id)
        {
            var clase = await _context.Tab_clas
                .FirstOrDefaultAsync(c => c.Id == id);
                
            if (clase == null) return NotFound();

            // Obtener información del usuario para mostrar
            var usuario = await _context.Tab_usr.FindAsync(clase.UsuarioId);
            ViewBag.UsuarioNombre = usuario?.Nombre;

            return View(clase);
        }

        [HttpPost]
        [ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var clase = await _context.Tab_clas.FindAsync(id);
            if (clase != null)
            {
                _context.Tab_clas.Remove(clase);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Clase eliminada exitosamente";
            }
            return RedirectToAction("Index");
        }

        private bool ClaseExists(int id)
        {
            return _context.Tab_clas.Any(e => e.Id == id);
        }
    }
}