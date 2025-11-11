using EscuelaCore.Enums;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Dto.RequestsDto
{
    public class UpdateMatriculaRequestDto
    {
        [Required(ErrorMessage = "El ID del alumno es requerido")]
        public int AlumnoId { get; set; }

        [Required(ErrorMessage = "El ID del aula es requerido")]
        public int AulaId { get; set; }

        [Required(ErrorMessage = "La carrera es requerida")]
        [EnumDataType(typeof(Carrera), ErrorMessage = "La carrera no es válida")]
        public Carrera Carrera { get; set; }
    }
}
