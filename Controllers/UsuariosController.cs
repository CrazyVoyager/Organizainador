using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Organizainador.Data;
using Organizainador.Models;
using Microsoft.Extensions.Logging;

namespace Organizainador.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsuariosController : Controller
    {                                                                               
        private readonly AppDbContext _context;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(AppDbContext context, ILogger<UsuariosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ======================== LISTADO PRINCIPAL ========================
        [HttpGet]
        public async Task<IActionResult> Index(string? busqueda, string? ordenar)
        {
            try
            {
                var query = _context.Usuarios.AsQueryable();

                // Búsqueda
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    query = query.Where(u =>
                        u.Nombre.Contains(busqueda) ||
                        u.Email.Contains(busqueda) ||
                        (u.CEst != null && u.CEst.Contains(busqueda))
                    );
                    ViewData["BusquedaActual"] = busqueda;
                }

                // Ordenamiento
                query = ordenar switch
                {
                    "nombre_desc" => query.OrderByDescending(u => u.Nombre),
                    "email" => query.OrderBy(u => u.Email),
                    "email_desc" => query.OrderByDescending(u => u.Email),
                    _ => query.OrderBy(u => u.Nombre)
                };

                var usuarios = await query.ToListAsync();
                return View(usuarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la lista de usuarios");
                TempData["ErrorMessage"] = "Ocurrió un error al cargar los usuarios.";
                return View(new List<UsuarioModel>());
            }
        }

        // ======================== DETALLE DE USUARIO ========================
        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                // Obtener estadísticas del usuario
                var cantidadClases = await _context.Clases
                    .Where(c => c.UsuarioId == id)
                    .CountAsync();

                var cantidadActividades = await _context.Actividades
                    .Where(a => a.UsuarioId == id)
                    .CountAsync();

                ViewData["CantidadClases"] = cantidadClases;
                ViewData["CantidadActividades"] = cantidadActividades;

                return View(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el detalle del usuario {Id}", id);
                TempData["ErrorMessage"] = "Error al cargar el detalle del usuario.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ======================== CREAR USUARIO ========================
        [HttpGet]
        public IActionResult Crear()
        {
            return View(new UsuarioModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(UsuarioModel usuario)
        {
            try
            {
                // Validación personalizada del email
                if (await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email))
                {
                    ModelState.AddModelError("Email", "Este correo electrónico ya está registrado.");
                }

                if (!ModelState.IsValid)
                {
                    return View(usuario);
                }

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuario creado: {Email}", usuario.Email);
                TempData["SuccessMessage"] = $"Usuario '{usuario.Nombre}' creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario");
                ModelState.AddModelError("", "Ocurrió un error al crear el usuario. Por favor, intenta nuevamente.");
                return View(usuario);
            }
        }

        // ======================== EDITAR USUARIO ========================
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado.";
                    return RedirectToAction(nameof(Index));
                }
                return View(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el usuario {Id} para editar", id);
                TempData["ErrorMessage"] = "Error al cargar el usuario.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, UsuarioModel usuario)
        {
            if (id != usuario.Id)
            {
                return NotFound();
            }

            try
            {
                // Validar email único (excluyendo el usuario actual)
                if (await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email && u.Id != id))
                {
                    ModelState.AddModelError("Email", "Este correo electrónico ya está registrado.");
                }

                if (!ModelState.IsValid)
                {
                    return View(usuario);
                }

                _context.Update(usuario);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuario actualizado: {Id} - {Email}", usuario.Id, usuario.Email);
                TempData["SuccessMessage"] = $"Usuario '{usuario.Nombre}' actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await UsuarioExistsAsync(usuario.Id))
                {
                    TempData["ErrorMessage"] = "El usuario ya no existe.";
                    return RedirectToAction(nameof(Index));
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario {Id}", id);
                ModelState.AddModelError("", "Ocurrió un error al actualizar el usuario.");
                return View(usuario);
            }
        }

        // ======================== ELIMINAR USUARIO ========================
        [HttpGet]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar dependencias
                var tieneClases = await _context.Clases.AnyAsync(c => c.UsuarioId == id);
                var tieneActividades = await _context.Actividades.AnyAsync(a => a.UsuarioId == id);

                ViewData["TieneClases"] = tieneClases;
                ViewData["TieneActividades"] = tieneActividades;
                ViewData["PuedeEliminar"] = !tieneClases && !tieneActividades;

                return View(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el usuario {Id} para eliminar", id);
                TempData["ErrorMessage"] = "Error al cargar el usuario.";
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
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar que no tenga dependencias
                var tieneClases = await _context.Clases.AnyAsync(c => c.UsuarioId == id);
                var tieneActividades = await _context.Actividades.AnyAsync(a => a.UsuarioId == id);

                if (tieneClases || tieneActividades)
                {
                    TempData["ErrorMessage"] = "No se puede eliminar el usuario porque tiene clases o actividades asociadas.";
                    return RedirectToAction(nameof(Eliminar), new { id });
                }

                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuario eliminado: {Id} - {Email}", usuario.Id, usuario.Email);
                TempData["SuccessMessage"] = $"Usuario '{usuario.Nombre}' eliminado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario {Id}", id);
                TempData["ErrorMessage"] = "Error al eliminar el usuario.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ======================== MÉTODOS AUXILIARES ========================
        private async Task<bool> UsuarioExistsAsync(int id)
        {
            return await _context.Usuarios.AnyAsync(e => e.Id == id);
        }
    }
}