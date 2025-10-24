using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Organizainador.Models
{
    [Table("tab_usr")] // Mapea con tu tabla de PostgreSQL
    public class UsuarioModel
    {
        [Key]
        [Column("tus_id_usr")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Column("tus_nom")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [Column("tus_mail")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Column("tus_c_est")]
        [Display(Name = "Casa de estudios")]
        public string CEst { get; set; } = "ACTIVO";

        [Column("tus_est")]
        [Display(Name = "Estado")]
        public string Est { get; set; } = "ACTIVO";
    }
}