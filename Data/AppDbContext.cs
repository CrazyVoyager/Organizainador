using Microsoft.EntityFrameworkCore;
using Organizainador.Models;

namespace Organizainador.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<UsuarioModel> Tab_usr { get; set; }
        public DbSet<ClaseModel> Tab_clas { get; set; }
    }
}