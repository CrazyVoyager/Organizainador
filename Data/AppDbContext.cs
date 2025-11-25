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
            base.OnModelCreating(modelBuilder);

            // Configuración de ClaseModel
            modelBuilder.Entity<ClaseModel>(entity =>
            {
                entity.ToTable("tab_clas");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id)
                    .HasColumnName("tcl_id_clas")
                    .ValueGeneratedOnAdd();
                
                entity.Property(e => e.UsuarioId)
                    .HasColumnName("tus_id_usr")
                    .IsRequired();
                
                entity.Property(e => e.Nombre)
                    .HasColumnName("tcl_nom_clas")
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.Descripcion)
                    .HasColumnName("tcl_desc")
                    .HasMaxLength(500);

                // ⭐ CONFIGURACIÓN DE tcl_cant_h_d: siempre será 1, el usuario no lo verá
                entity.Property(e => e.CantidadHorasDia)
                    .HasColumnName("tcl_cant_h_d")
                    .HasDefaultValue(1)
                    .IsRequired();

                // Relación con Usuario
                entity.HasOne<UsuarioModel>()
                    .WithMany()
                    .HasForeignKey(c => c.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de HorarioModel
            modelBuilder.Entity<HorarioModel>(entity =>
            {
                entity.ToTable("tab_hor");

                entity.Property(e => e.Id).HasColumnName("tho_id_hor");
                entity.Property(e => e.ClaseId).HasColumnName("tcl_id_clas");
                entity.Property(e => e.ActividadId).HasColumnName("tac_id_act");
                entity.Property(e => e.DiaSemana).HasColumnName("tho_d_sem");
                entity.Property(e => e.HoraInicio).HasColumnName("tho_h_ini");
                entity.Property(e => e.HoraFin).HasColumnName("tho_h_fin");
                entity.Property(e => e.EsRecurrente).HasColumnName("tho_recurrente");
                entity.Property(e => e.FechaEspecifica).HasColumnName("tho_fecha_especifica");

                // Relación con Clase
                entity.HasOne(h => h.Clase)
                    .WithMany(c => c.Horarios)
                    .HasForeignKey(h => h.ClaseId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired(false);

                // Relación con Actividad
                entity.HasOne(h => h.Actividad)
                    .WithMany()
                    .HasForeignKey(h => h.ActividadId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired(false);
            });

            // Configuración de ActividadModel
            modelBuilder.Entity<ActividadModel>()
                .HasOne<UsuarioModel>()
                .WithMany()
                .HasForeignKey(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}