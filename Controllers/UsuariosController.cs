using Microsoft.AspNetCore.Mvc;
using Organizainador.Models;

namespace Organizainador.Controllers
{
    public class UsuariosController : Controller
    {
        private static List<UsuarioModel> usuarios = new();
        private static int nextUsuarioId = 1;

        public static List<UsuarioModel> GetUsuariosStatic()
        {
            return usuarios;
        }

        // ======================== LISTADO PRINCIPAL ========================
        [HttpGet]
        public IActionResult Index()
        {
            return View(usuarios);
        }

        // ======================== CREAR USUARIO ========================
        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Crear(UsuarioModel usuario)
        {
            if (ModelState.IsValid)
            {
                // Verificar si el email ya existe
                if (usuarios.Any(u => u.Email == usuario.Email))
                {
                    ModelState.AddModelError("Email", "Este email ya está registrado");
                    return View(usuario);
                }

                usuario.Id = nextUsuarioId++;
                usuario.CEst = "ACTIVO"; // Valor por defecto
                usuario.Est = "ACTIVO";  // Valor por defecto
                usuarios.Add(usuario);
                return RedirectToAction("Index");
            }
            return View(usuario);
        }

        // ======================== MODIFICAR USUARIO ========================
        [HttpGet]
        public IActionResult Modificar(int id)
        {
            var usuario = usuarios.FirstOrDefault(u => u.Id == id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        [HttpPost]
        public IActionResult Modificar(int id, UsuarioModel usuario)
        {
            if (ModelState.IsValid)
            {
                var usuarioExistente = usuarios.FirstOrDefault(u => u.Id == id);
                if (usuarioExistente == null) return NotFound();

                // Verificar si el email ya existe (excluyendo el usuario actual)
                if (usuarios.Any(u => u.Email == usuario.Email && u.Id != id))
                {
                    ModelState.AddModelError("Email", "Este email ya está registrado");
                    return View(usuario);
                }

                usuarioExistente.Nombre = usuario.Nombre;
                usuarioExistente.Email = usuario.Email;
                usuarioExistente.Contrasena = usuario.Contrasena;
                usuarioExistente.CEst = usuario.CEst;
                usuarioExistente.Est = usuario.Est;

                return RedirectToAction("Index");
            }
            return View(usuario);
        }

        // ======================== ELIMINAR USUARIO ========================
        [HttpGet]
        public IActionResult Eliminar(int id)
        {
            var usuario = usuarios.FirstOrDefault(u => u.Id == id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        [HttpPost]
        [ActionName("Eliminar")]
        public IActionResult EliminarConfirmado(int id)
        {
            var usuario = usuarios.FirstOrDefault(u => u.Id == id);
            if (usuario != null)
            {
                usuarios.Remove(usuario);
            }
            return RedirectToAction("Index");
        }
    }
}