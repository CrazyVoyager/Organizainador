using Microsoft.AspNetCore.Mvc;
using Organizainador.Models;

namespace Organizainador.Controllers
{
    public class PruebaController : Controller
    {
        [HttpGet]
        public IActionResult Prueba()
        {
            // Esto busca la vista "Prueba.cshtml" en las carpetas Views/Prueba o Views/PruebaController
            return View("Prueba");
        }
    }
}
