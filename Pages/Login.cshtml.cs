using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Organizainador.Services;
using Microsoft.AspNetCore.Authorization;
using Organizainador.Pages; // Asegúrate de tener este using si tu modelo está fuera del namespace principal

namespace Organizainador.Pages
{
    // CLAVE: Indica que esta PageModel acepta solicitudes JSON en el cuerpo.
    [Consumes("application/json")]
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly UserService _userService;

        // La propiedad Input ahora es opcional para que la vista pueda renderizar
        public LoginInputModel? Input { get; set; }

        public LoginModel(UserService userService)
        {
            _userService = userService;
        }

        // Modelo para deserializar el JSON que viene del JavaScript
        public class LoginInputModel
        {
            // Las propiedades se mantienen como cadenas no nulas
            public string Email { get; set; } = string.Empty;
            public string Contrasena { get; set; } = string.Empty;
        }

        public void OnGet()
        {
            // El operador ! es seguro aquí porque Identity solo puede ser null 
            // si el usuario no está autenticado, y en ese caso, IsAuthenticated sería false.
            if (User.Identity!.IsAuthenticated)
            {
                Response.Redirect("/PaginaPrincipal");
            }
        }

        // POST /Login?handler=Auth
        // NOTA: En Razor Pages, ValidateAntiForgeryToken se aplica a nivel de página, no de handler
        public async Task<IActionResult> OnPostAuth([FromBody] LoginInputModel? data)
        {
            // 1. Verificar si el Model Binding falló (si data es null)
            if (data == null || string.IsNullOrEmpty(data.Email) || string.IsNullOrEmpty(data.Contrasena))
            {
                // Devolver BadRequest si el JSON no se pudo mapear
                return new BadRequestResult();
            }

            // 2. Llamar al UserService y obtener el UserDto completo.
            var user = await _userService.ValidateCredentialsAsync(data.Email, data.Contrasena);

            if (user != null)
            {
                // AUTENTICACIÓN EXITOSA
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId),
                    new Claim(ClaimTypes.Name, user.Email),
                    // CLAVE 2: Usar el rol dinámico obtenido de la base de datos
                    new Claim(ClaimTypes.Role, user.Role),
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return new OkResult(); // 200 OK
            }
            else
            {
                // 401 Unauthorized: Credenciales incorrectas (no se encontró en la BD)
                return new UnauthorizedResult();
            }
        }
    }
}