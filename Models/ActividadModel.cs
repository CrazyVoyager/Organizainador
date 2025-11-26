using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Organizainador.Models
{
    // Mapea con la tabla 'tab_act'
    [Table("tab_act")]
    public class ActividadModel
    {
        [Key]
        [Column("tac_id_act")]
        [Display(Name = "ID Actividad")]
        public int Id { get; set; }

        [Column("tus_id_usr")]
        [Display(Name = "ID Usuario")]
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "El nombre de la actividad es obligatorio")]
        [Column("tac_nom_act")]
        [Display(Name = "Nombre de la Actividad")]
        [StringLength(500)]
        public string Nombre { get; set; } = string.Empty;

        [Column("tac_desc")]
        [Display(Name = "Descripción")]
        [StringLength(2000)]
        public string? Descripcion { get; set; }  // Nullable - Opcional

        [Column("tac_t_act")]
        [Display(Name = "Tipo/Etiqueta")]
        [StringLength(100)]
        public string? Etiqueta { get; set; }  // Nullable - Opcional

        [Column("created_at")]
        [Display(Name = "Fecha de Creación")]
        public DateTime? CreatedAt { get; set; }
    }
}