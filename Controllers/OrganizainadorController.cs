using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Organizainador.Controllers
{
    public class OrganizainadorController : Controller
    {
        [HttpGet]
        public IActionResult PaginaPrincipal()
        {
            // Renderiza la vista PaginaPrincipal en Views/Organizainador/
            return View("PaginaPrincipal");
        }

        // ======================== ACCIÓN DE CERRAR SESIÓN ========================
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Limpia la cookie de autenticación
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirige a la ruta de inicio de sesión definida en Program.cs
            return Redirect("/Login");
        }
    }
}
