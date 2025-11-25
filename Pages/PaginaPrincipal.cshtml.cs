using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Organizainador.Pages
{
    public class PaginaPrincipalModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string Usuario { get; set; } = "Usuario";

        public void OnGet()
        {
            // El valor por defecto ya está establecido en la propiedad
        }
    }
}