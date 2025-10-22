using Microsoft.AspNetCore.Mvc;
using Organizainador.Models;

namespace Organizainador.Controllers
{
    public class ClasesController : Controller
    {
        private static List<ClaseModel> clases = new();
        private static int nextClaseId = 1;

        // ======================== LISTADO PRINCIPAL ========================
        [HttpGet]
        public IActionResult Index()
        {
            // Obtener usuarios para mostrar nombres
            var usuarios = UsuariosController.GetUsuariosStatic();
            var clasesConUsuario = clases.Select(c => new
            {
                Clase = c,
                Usuario = usuarios?.FirstOrDefault(u => u.Id == c.UsuarioId)
            }).ToList();

            ViewBag.ClasesConUsuario = clasesConUsuario;
            return View(clases);
        }

        // ======================== CREAR CLASE ========================
        [HttpGet]
        public IActionResult Crear()
        {
            ViewBag.Usuarios = UsuariosController.GetUsuariosStatic();
            return View();
        }

        [HttpPost]
        public IActionResult Crear(ClaseModel clase)
        {
            if (ModelState.IsValid)
            {
                // Verificar que el usuario existe
                var usuario = UsuariosController.GetUsuariosStatic()?.FirstOrDefault(u => u.Id == clase.UsuarioId);
                if (usuario == null)
                {
                    ModelState.AddModelError("UsuarioId", "El usuario seleccionado no existe");
                    ViewBag.Usuarios = UsuariosController.GetUsuariosStatic();
                    return View(clase);
                }

                clase.Id = nextClaseId++;
                clases.Add(clase);
                return RedirectToAction("Index");
            }

            ViewBag.Usuarios = UsuariosController.GetUsuariosStatic();
            return View(clase);
        }

        // ======================== MODIFICAR CLASE ========================
        [HttpGet]
        public IActionResult Modificar(int id)
        {
            var clase = clases.FirstOrDefault(c => c.Id == id);
            if (clase == null) return NotFound();

            ViewBag.Usuarios = UsuariosController.GetUsuariosStatic();
            return View(clase);
        }

        [HttpPost]
        public IActionResult Modificar(int id, ClaseModel clase)
        {
            if (ModelState.IsValid)
            {
                var claseExistente = clases.FirstOrDefault(c => c.Id == id);
                if (claseExistente == null) return NotFound();

                // Verificar que el usuario existe
                var usuario = UsuariosController.GetUsuariosStatic()?.FirstOrDefault(u => u.Id == clase.UsuarioId);
                if (usuario == null)
                {
                    ModelState.AddModelError("UsuarioId", "El usuario seleccionado no existe");
                    ViewBag.Usuarios = UsuariosController.GetUsuariosStatic();
                    return View(clase);
                }

                claseExistente.UsuarioId = clase.UsuarioId;
                claseExistente.Nombre = clase.Nombre;
                claseExistente.Descripcion = clase.Descripcion;
                claseExistente.CantidadHorasDia = clase.CantidadHorasDia;

                return RedirectToAction("Index");
            }

            ViewBag.Usuarios = UsuariosController.GetUsuariosStatic();
            return View(clase);
        }

        // ======================== ELIMINAR CLASE ========================
        [HttpGet]
        public IActionResult Eliminar(int id)
        {
            var clase = clases.FirstOrDefault(c => c.Id == id);
            if (clase == null) return NotFound();

            // Obtener información del usuario para mostrar
            var usuario = UsuariosController.GetUsuariosStatic()?.FirstOrDefault(u => u.Id == clase.UsuarioId);
            ViewBag.UsuarioNombre = usuario?.Nombre;

            return View(clase);
        }

        [HttpPost]
        [ActionName("Eliminar")]
        public IActionResult EliminarConfirmado(int id)
        {
            var clase = clases.FirstOrDefault(c => c.Id == id);
            if (clase != null)
            {
                clases.Remove(clase);
            }
            return RedirectToAction("Index");
        }

        // ======================== MÉTODOS AUXILIARES ========================
        public static List<ClaseModel> GetClasesStatic()
        {
            return clases;
        }
    }
}