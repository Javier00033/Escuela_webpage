using EscuelaCore.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Dto.RequestsDto
{
    public class UpdateAlumnoRequestDto
    {
        [Required(ErrorMessage = "Introduzca el nombre"), MaxLength(250)]
        [RegularExpression(@"^([A-ZÁÉÍÓÚÑ][a-záéíóúñü'-]*)(\s[A-ZÁÉÍÓÚÑ][a-záéíóúñü'-]*)*$", ErrorMessage = "Cada palabra debe comenzar con mayúscula y solo contener letras válidas")]
        [DefaultValue("Nombre")]
        public required string Nombre { get; set; }

        [Required(ErrorMessage = "Introduzca los apellidos"), MaxLength(250)]
        [RegularExpression(@"^([A-ZÁÉÍÓÚÑ][a-záéíóúñü'-]*)(\s[A-ZÁÉÍÓÚÑ][a-záéíóúñü'-]*)*$", ErrorMessage = "Cada palabra debe comenzar con mayúscula y solo contener letras válidas")]
        [DefaultValue("Apellidos")]
        public required string Apellidos { get; set; }

        [Required(ErrorMessage = "Introduzca el CI")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "El CI debe tener exactamente 11 dígitos")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "El CI solo puede contener números")]
        [DefaultValue("CI")]
        public required string CI { get; set; }
    }
}
