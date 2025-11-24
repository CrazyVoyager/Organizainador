using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace Organizainador.Models
{
    [Table("tab_hor")]
    public class HorarioModel
    {
        [Key]
        [Column("tho_id_hor")]
        public int Id { get; set; }

        [Column("tcl_id_clas")]
        public int? ClaseId { get; set; }

        [Column("tac_id_act")]
        public int? ActividadId { get; set; }

        [Column("tho_d_sem")]
        public string? DiaSemana { get; set; }

        [Column("tho_fecha")]
        public DateTime? Fecha { get; set; }

        [Required(ErrorMessage = "La hora de inicio es obligatoria")]
        [Column("tho_h_ini")]
        public TimeSpan HoraInicio { get; set; }

        [Required(ErrorMessage = "La hora de fin es obligatoria")]
        [Column("tho_h_fin")]
        public TimeSpan HoraFin { get; set; }

        // Propiedades de navegación
        [ForeignKey("ClaseId")]
        public ClaseModel? Clase { get; set; }

        [ForeignKey("ActividadId")]
        public ActividadModel? Actividad { get; set; }
    }
}