using EscuelaCore.Enums;
using EscuelaCore.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Dto.RequestsDto
{
    public class CreateProfesorRequestDto
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

        [Required(ErrorMessage = "Introduzca el email"), MaxLength(250)]
        [RegularExpression(@"^\w+([.-]?\w+)*@\w+([.-]?\w+)*(\.\w{2,3})+$", ErrorMessage = "El email debe tener un formato correcto con minusculas, sin acentos, apóstrofes ni caracteres exclusivos")]
        [DefaultValue("email@escuela.com")]
        public required string UsuarioEmail { get; set; }

        [Required(ErrorMessage = "Introduzca la contraseña")]
        public required string Contraseña { get; set; }

        [Required(ErrorMessage = "Introduzca la asignatura")]
        [EnumDataType(typeof(Asignatura), ErrorMessage = "La asignatura no es válida")]
        public Asignatura Asignatura { get; set; }

        [Required(ErrorMessage = "Introduzca la(s) aula(s)")]
        public List<CreateAulaProfesorRequestDto> AulaProfesores { get; set; } = new List<CreateAulaProfesorRequestDto>();
    }
}
