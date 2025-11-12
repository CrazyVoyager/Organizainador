using Microsoft.EntityFrameworkCore;
using Organizainador.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Organizainador.Data
{
    // Heredamos de IdentityDbContext para mantener la funcionalidad de Identity (si la usas)
    // y manejamos nuestros modelos personalizados.
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // --- DBSETs FALTANTES Y EXISTENTES ---

        // Usuarios (tab_usr)
        public DbSet<UsuarioModel> Usuarios { get; set; } // Renombrado a Usuarios para claridad

        // Clases (tab_clas)
        public DbSet<ClaseModel> Clases { get; set; } // Renombrado a Clases para claridad

        // Actividades (tab_act)
        public DbSet<ActividadModel> Actividades { get; set; } // Renombrado a Actividades para claridad

        // Horarios (tab_hor) - ¡FALTANTE! Es crucial para las clases recurrentes
        public DbSet<HorarioModel> Horarios { get; set; }


        // --- CONFIGURACIÓN DE RELACIONES Y MAPEOS DE TABLAS/COLUMNAS ---

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // NECESARIO para que las tablas de Identity se creen correctamente

            // 1. Mapeo explícito de HorarioModel (Faltaban atributos [Table] y [Column] en el modelo)
            modelBuilder.Entity<HorarioModel>(entity =>
            {
                entity.ToTable("tab_hor"); // Define el nombre de la tabla

                // Mapeo de columnas explícito
                entity.Property(e => e.Id).HasColumnName("tho_id_hor");
                entity.Property(e => e.ClaseId).HasColumnName("tcl_id_clas"); // Clave foránea
                entity.Property(e => e.DiaSemana).HasColumnName("tho_d_sem");
                entity.Property(e => e.HoraInicio).HasColumnName("tho_h_ini");
                entity.Property(e => e.HoraFin).HasColumnName("tho_h_fin");

                // Relación: Un Horario pertenece a una Clase (1:N)
                entity.HasOne(h => h.Clase)
                      .WithMany(c => c.Horarios) // Asegúrate de añadir `public ICollection<HorarioModel>? Horarios { get; set; }` en ClaseModel
                      .HasForeignKey(h => h.ClaseId)
                      .IsRequired();
            });

            // 2. Relación Usuario (tab_usr) y Actividad (tab_act)
            modelBuilder.Entity<ActividadModel>()
                .HasOne<UsuarioModel>() // Una Actividad tiene un Usuario
                .WithMany()
                .HasForeignKey(a => a.UsuarioId) // Mapea a la columna 'tus_id_usr'
                .IsRequired();

            // 3. Relación Usuario (tab_usr) y Clase (tab_clas)
            modelBuilder.Entity<ClaseModel>()
                .HasOne<UsuarioModel>() // Una Clase tiene un Usuario
                .WithMany()
                .HasForeignKey(c => c.UsuarioId) // Mapea a la columna 'tus_id_usr'
                .IsRequired();

            // 4. Configuración Adicional para PostgreSQL con TimeSpan
            // Esto es útil si usas Npgsql, ya que TimeSpan a veces necesita precisión.
            // Si experimentas errores con las columnas HoraInicio y HoraFin, descomenta esto:
            /*
            modelBuilder.Entity<HorarioModel>(entity =>
            {
                entity.Property(e => e.HoraInicio)
                    .HasColumnType("interval"); // Tipo de dato PostgreSQL para TimeSpan
                entity.Property(e => e.HoraFin)
                    .HasColumnType("interval");
            });
            */
        }
    }
}