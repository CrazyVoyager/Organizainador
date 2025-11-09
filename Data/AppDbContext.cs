using Microsoft.EntityFrameworkCore;
using Organizainador.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // <-- NEW: Necesario para Identity
using Microsoft.AspNetCore.Identity; // <-- NEW: Necesario para IdentityUser

namespace Organizainador.Data
{
    // CAMBIO CLAVE: Cambiamos la herencia de 'DbContext' a 'IdentityDbContext<IdentityUser>'
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        // El constructor ahora usa el tipo 'AppDbContext' para su DbContextOptions
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<UsuarioModel> Tab_usr { get; set; }
        public DbSet<ClaseModel> Tab_clas { get; set; }
        // Las tablas de Identity (AspNetUsers, etc.) se añaden automáticamente por la herencia.
    }
}
