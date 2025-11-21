using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Organizainador.Pages
{
    public class PaginaPrincipalModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string Usuario { get; set; } = string.Empty;

        public void OnGet()
        {
            // Si no viene por par�metro, establecer un valor por defecto
            if (string.IsNullOrEmpty(Usuario))
            {
                Usuario = "Usuario";
            }
        }
    }
}