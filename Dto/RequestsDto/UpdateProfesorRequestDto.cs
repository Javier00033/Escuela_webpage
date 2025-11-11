using EscuelaCore.Enums;
using EscuelaCore.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Dto.RequestsDto
{
    public class UpdateProfesorRequestDto
    {
        [Required(ErrorMessage = "Introduzca el nombre"), MaxLength(250)]
        [RegularExpression(@"^([A-ZÁÉÍÓÚÑ][a-záéíóúñü'-]*)(\s[A-ZÁÉÍÓÚÑ][a-záéíóúñü'-]*)*$", ErrorMessage = "Cada palabra debe comenzar con mayúscula y solo contener letras válidas")]
        [DefaultValue("Nombre")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Introduzca los apellidos"), MaxLength(250)]
        [RegularExpression(@"^([A-ZÁÉÍÓÚÑ][a-záéíóúñü'-]*)(\s[A-ZÁÉÍÓÚÑ][a-záéíóúñü'-]*)*$", ErrorMessage = "Cada palabra debe comenzar con mayúscula y solo contener letras válidas")]
        [DefaultValue("Apellidos")]
        public string Apellidos { get; set; }

        [Required(ErrorMessage = "Introduzca el CI")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "El CI debe tener exactamente 11 dígitos")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "El CI solo puede contener números")]
        [DefaultValue("CI")]
        public string CI { get; set; }

        [Required(ErrorMessage = "Introduzca la asignatura")]
        [EnumDataType(typeof(Asignatura), ErrorMessage = "La asignatura no es válida")]
        public Asignatura Asignatura { get; set; }

        [Required(ErrorMessage = "Introduzca la(s) aula(s)")]
        public List<CreateAulaProfesorRequestDto> AulaProfesores { get; set; } = new List<CreateAulaProfesorRequestDto>();
    }
}
