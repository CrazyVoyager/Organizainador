using Microsoft.AspNetCore.Mvc;
using Organizainador.Models;

namespace Organizainador.Controllers
{
    public class PruebaController : Controller 
    {
        [HttpGet]
        public IActionResult Prueba()
        {
            
            return View("Prueba");
        }
    }
}
