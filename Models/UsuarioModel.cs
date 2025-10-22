using System.ComponentModel.DataAnnotations;

namespace Organizainador.Models
{
    public class UsuarioModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(255, ErrorMessage = "La contraseña no puede exceder 255 caracteres")]
        public string Contrasena { get; set; }

        [StringLength(100)]
        public string? CEst { get; set; } // Campo adicional según tab_usr

        [StringLength(100)]
        public string? Est { get; set; } // Campo adicional según tab_usr
    }
}