using System;
using System.Collections.Generic;
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
        public string Nombre { get; set; }

        [Column("tac_desc")]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }

        [Column("tac_t_act")]
        [Display(Name = "Tipo/Etiqueta")]
        public string Etiqueta { get; set; }

        // Propiedad de navegación para horarios
        public ICollection<HorarioModel>? Horarios { get; set; }
    }
}