using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Organizainador.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "El usuario es obligatorio")]
        public string Usuario { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        public string Contrasena { get; set; }

        public string MensajeError { get; set; }

        public void OnGet()
        {
            // inicializaciones si las necesitas
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // EJEMPLO de validación simple (reemplaza por tu lógica real)
            if (Usuario == "admin" && Contrasena == "1234")
            {
                // Redirigir a la misma Razor Page PaginaPrincipal
                return RedirectToPage("/PaginaPrincipal", new { usuario = Usuario });
            }

            MensajeError = "Usuario o contraseña incorrectos";
            return Page();
        }
    }
}