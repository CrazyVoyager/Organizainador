using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic; // Para usar ICollection si es necesario, aunque no lo es aquí

namespace Organizainador.Models
{
    [Table("tab_hor")]
    public class HorarioModel
    {
        [Key]
        [Column("tho_id_hor")]
        public int Id { get; set; }

        [Required]
        [Column("tcl_id_clas")]
        public int ClaseId { get; set; }

        [Required(ErrorMessage = "El día de la semana es obligatorio")]
        [Column("tho_d_sem")]
        public string DiaSemana { get; set; } = string.Empty; 

        [Required(ErrorMessage = "La hora de inicio es obligatoria")]
        [Column("tho_h_ini")]
        public TimeSpan HoraInicio { get; set; } 

        [Required(ErrorMessage = "La hora de fin es obligatoria")]
        [Column("tho_h_fin")]
        public TimeSpan HoraFin { get; set; } 

        // Propiedad de navegación
        // Permite cargar el objeto ClaseModel asociado sin hacer JOIN manual
        [ForeignKey("ClaseId")]
        public ClaseModel? Clase { get; set; }
    }
}