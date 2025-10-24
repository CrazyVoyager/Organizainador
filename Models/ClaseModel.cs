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
        [Column("tus_id_usr")] // ✅ CORREGIDO: Nombre real de la columna FK
        [Display(Name = "Usuario")]
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Column("tcl_nom_clas")]
        [Display(Name = "Nombre de la clase")]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Column("tcl_desc")]
        [Display(Name = "Descripción")]
        [StringLength(500)]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "La cantidad de horas es obligatoria")]
        [Column("tcl_cant_h_d")]
        [Display(Name = "Cantidad de horas por día")]
        [Range(1, 24, ErrorMessage = "La cantidad debe estar entre 1 y 24 horas")]
        public int CantidadHorasDia { get; set; } = 1;
    }
}