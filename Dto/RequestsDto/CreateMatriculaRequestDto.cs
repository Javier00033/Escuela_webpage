using EscuelaCore.Enums;
using EscuelaCore.Models;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Dto.RequestsDto
{
    public class CreateMatriculaRequestDto
    {
        [Required(ErrorMessage = "Introduzca el id del alumno")]
        public int AlumnoId { get; set; }

        [Required(ErrorMessage = "Introduzca el id del aula")]
        public int AulaId { get; set; }

        [Required(ErrorMessage = "Introduzca la carrera")]
        [EnumDataType(typeof(Carrera), ErrorMessage = "La carrera no es válida")]
        public Carrera Carrera { get; set; }
    }
}
