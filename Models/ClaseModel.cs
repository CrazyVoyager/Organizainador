using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Organizainador.Models
{
    [Table("tab_clas")]
    public class ClaseModel
    {
        [Key]
        [Column("tcl_id_clas")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "El usuario es obligatorio")]
        [Column("tus_id_usr")]
        [Display(Name = "Usuario")]
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Column("tcl_nom_clas")]
        [Display(Name = "Nombre de la clase")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        [MinLength(1, ErrorMessage = "El nombre debe tener al menos 1 carácter")]
        public string Nombre { get; set; } = string.Empty;

        [Column("tcl_desc")]
        [Display(Name = "Descripción")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres")]
        public string? Descripcion { get; set; }

        // ⭐ NUEVA PROPIEDAD: Columna tcl_cant_h_d - Oculta para el usuario, siempre será 1
        [Column("tcl_cant_h_d")]
        public int CantidadHorasDia { get; set; } = 1;

        // Navegación - no se mapea a columna
        [NotMapped] // Esto evita que EF intente mapear esta propiedad a una columna
        public ICollection<HorarioModel>? Horarios { get; set; }
    }
}