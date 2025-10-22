using System.ComponentModel.DataAnnotations;

namespace Organizainador.Models
{
    public class ClaseModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El ID de usuario es obligatorio")]
        [Display(Name = "Usuario")]
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "El nombre de la clase es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Display(Name = "Nombre de la Clase")]
        public string Nombre { get; set; }

        [StringLength(255, ErrorMessage = "La descripción no puede exceder 255 caracteres")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "La cantidad de horas es obligatoria")]
        [Range(0.01, 99.99, ErrorMessage = "La cantidad debe estar entre 0.01 y 99.99")]
        [Display(Name = "Horas por Día")]
        public decimal CantidadHorasDia { get; set; }

        // Propiedad de navegación (para mostrar el nombre del usuario)
        public UsuarioModel? Usuario { get; set; }
    }
}