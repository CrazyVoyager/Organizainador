using Microsoft.AspNetCore.Mvc;

namespace Organizainador.Controllers
{
    public class OrganizainadorController : Controller
    {

        [HttpGet]
        public IActionResult PaginaPrincipal()
        {
            // Esto busca la vista "Prueba.cshtml" en las carpetas Views/Prueba o Views/PruebaController
            return View("PaginaPrincipal");
        }


    }
}
