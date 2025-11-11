using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Dto.RequestsDto
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Introduzca el email")]
        [DefaultValue("admin@admin.com")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Introduzca la contraseña")]
        [DefaultValue("Admin123!")]
        public string Password { get; set; }
    }
}
