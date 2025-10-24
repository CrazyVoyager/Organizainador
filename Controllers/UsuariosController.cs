using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Organizainador.Models;
using Organizainador.Data; // Asegúrate de tener este using para el DbContext

namespace Organizainador.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly AppDbContext _context;

        // Inyectamos el DbContext en el constructor
        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        // ======================== LISTADO PRINCIPAL ========================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Tab_usr.ToListAsync();
            return View(usuarios);
        }

        // ======================== CREAR USUARIO ========================
        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(UsuarioModel usuario)
        {
            if (ModelState.IsValid)
            {
                // Verificar si el email ya existe en la base de datos
                if (await _context.Tab_usr.AnyAsync(u => u.Email == usuario.Email))
                {
                    ModelState.AddModelError("Email", "Este email ya está registrado");
                    return View(usuario);
                }

                // Los valores por defecto se asignan automáticamente en el modelo
                _context.Tab_usr.Add(usuario);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Usuario creado exitosamente";
                return RedirectToAction("Index");
            }
            return View(usuario);
        }

        // ======================== MODIFICAR USUARIO ========================
        [HttpGet]
        public async Task<IActionResult> Modificar(int id)
        {
            var usuario = await _context.Tab_usr.FindAsync(id);
            if (usuario == null) 
            {
                return NotFound();
            }
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Modificar(int id, UsuarioModel usuario)
        {
            if (id != usuario.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Verificar si el email ya existe (excluyendo el usuario actual)
                if (await _context.Tab_usr.AnyAsync(u => u.Email == usuario.Email && u.Id != id))
                {
                    ModelState.AddModelError("Email", "Este email ya está registrado");
                    return View(usuario);
                }

                try
                {
                    _context.Update(usuario);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Usuario modificado exitosamente";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsuarioExists(usuario.Id))
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
            return View(usuario);
        }

        // ======================== ELIMINAR USUARIO ========================
        [HttpGet]
        public async Task<IActionResult> Eliminar(int id)
        {
            var usuario = await _context.Tab_usr
                .FirstOrDefaultAsync(u => u.Id == id);
                
            if (usuario == null) 
            {
                return NotFound();
            }
            return View(usuario);
        }

        [HttpPost]
        [ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var usuario = await _context.Tab_usr.FindAsync(id);
            if (usuario != null)
            {
                _context.Tab_usr.Remove(usuario);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Usuario eliminado exitosamente";
            }
            return RedirectToAction("Index");
        }

        private bool UsuarioExists(int id)
        {
            return _context.Tab_usr.Any(e => e.Id == id);
        }
    }
}