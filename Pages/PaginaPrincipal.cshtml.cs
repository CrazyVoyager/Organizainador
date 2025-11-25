using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Organizainador.Data;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Organizainador.Pages
{
    public class PaginaPrincipalModel : PageModel
    {
        private readonly AppDbContext _context;

        public PaginaPrincipalModel(AppDbContext context)
        {
            _context = context;
        }

        public string Usuario { get; set; } = "Usuario";

        public async Task OnGetAsync()
        {
            // Obtener el ID del usuario autenticado desde los Claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                // Buscar el usuario en la base de datos
                var usuarioDb = await _context.Usuarios
                    .Where(u => u.Id == userId)
                    .Select(u => u.Nombre)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(usuarioDb))
                {
                    Usuario = usuarioDb;
                }
            }
        }
    }
}