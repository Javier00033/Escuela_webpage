using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Dto.RequestsDto
{
    public class CreateAdminRequestDto
    {
        [Required(ErrorMessage = "Introduzca el email"), MaxLength(250)]
        [RegularExpression(@"^\w+([.-]?\w+)*@\w+([.-]?\w+)*(\.\w{2,3})+$", ErrorMessage = "El email debe tener un formato correcto con minusculas, sin acentos, apóstrofes ni caracteres exclusivos")]
        [DefaultValue("email@escuela.com")]
        public required string UsuarioEmail { get; set; }

        [Required(ErrorMessage = "Introduzca la contraseña")]
        public required string Contraseña { get; set; }
    }
}
