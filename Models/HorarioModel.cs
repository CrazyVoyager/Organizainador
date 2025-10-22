using System.ComponentModel.DataAnnotations;

namespace Organizainador.Models
{
    public class HorarioModel
    {
        public int Id { get; set; }

        [Required]
        public int ClaseId { get; set; } // FK tcl_id_class

        [Required(ErrorMessage = "El día de la semana es obligatorio")]
        public string DiaSemana { get; set; } // tho_d_sem

        [Required(ErrorMessage = "La hora de inicio es obligatoria")]
        public TimeSpan HoraInicio { get; set; } // tho_h_init

        [Required(ErrorMessage = "La hora de fin es obligatoria")]
        public TimeSpan HoraFin { get; set; } // tho_h_fin

        // Propiedad de navegación
        public ClaseModel? Clase { get; set; }
    }
}