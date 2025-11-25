using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace Organizainador.Models
{
    [Table("tab_hor")]
    public class HorarioModel : IValidatableObject
    {
        [Key]
        [Column("tho_id_hor")]
        public int Id { get; set; }

        [Column("tcl_id_clas")]
        public int? ClaseId { get; set; }

        [Column("tac_id_act")]
        public int? ActividadId { get; set; }

        // CAMBIO: Removemos [Required] porque no siempre es obligatorio
        [Column("tho_d_sem")]
        public string? DiaSemana { get; set; }

        [Required(ErrorMessage = "La hora de inicio es obligatoria")]
        [Column("tho_h_ini")]
        public TimeSpan HoraInicio { get; set; }

        [Required(ErrorMessage = "La hora de fin es obligatoria")]
        [Column("tho_h_fin")]
        public TimeSpan HoraFin { get; set; }

        [Column("tho_recurrente")]
        [Display(Name = "Es recurrente")]
        public bool EsRecurrente { get; set; } = true;

        [Column("tho_fecha_especifica")]
        [Display(Name = "Fecha específica")]
        public DateTime? FechaEspecifica { get; set; }

        [ForeignKey("ClaseId")]
        public ClaseModel? Clase { get; set; }

        [ForeignKey("ActividadId")]
        public ActividadModel? Actividad { get; set; }

        // ⭐ VALIDACIÓN PERSONALIZADA
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Validación: HoraFin debe ser posterior a HoraInicio
            if (HoraFin <= HoraInicio)
            {
                yield return new ValidationResult(
                    "La hora de fin debe ser posterior a la hora de inicio.",
                    new[] { nameof(HoraFin) }
                );
            }

            // Validación: Si es recurrente, DiaSemana es obligatorio
            if (EsRecurrente && string.IsNullOrWhiteSpace(DiaSemana))
            {
                yield return new ValidationResult(
                    "El día de la semana es obligatorio para eventos recurrentes.",
                    new[] { nameof(DiaSemana) }
                );
            }

            // Validación: Si NO es recurrente, FechaEspecifica es obligatoria
            if (!EsRecurrente && !FechaEspecifica.HasValue)
            {
                yield return new ValidationResult(
                    "Debes seleccionar una fecha específica para eventos no recurrentes.",
                    new[] { nameof(FechaEspecifica) }
                );
            }

            // Validación: La fecha específica no puede ser en el pasado
            if (!EsRecurrente && FechaEspecifica.HasValue && FechaEspecifica.Value.Date < DateTime.Now.Date)
            {
                yield return new ValidationResult(
                    "La fecha específica no puede ser anterior a hoy.",
                    new[] { nameof(FechaEspecifica) }
                );
            }

            // Validación: Debe tener ClaseId O ActividadId (no ambos, no ninguno)
            if (!ClaseId.HasValue && !ActividadId.HasValue)
            {
                yield return new ValidationResult(
                    "Debes asignar el horario a una Clase o una Actividad.",
                    new[] { nameof(ClaseId), nameof(ActividadId) }
                );
            }

            if (ClaseId.HasValue && ActividadId.HasValue)
            {
                yield return new ValidationResult(
                    "No puedes asignar el horario a una Clase y una Actividad al mismo tiempo.",
                    new[] { nameof(ClaseId), nameof(ActividadId) }
                );
            }
        }
    }
}