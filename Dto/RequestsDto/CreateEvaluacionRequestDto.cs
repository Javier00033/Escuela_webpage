using EscuelaCore.Enums;
using EscuelaCore.Models;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Dto.RequestsDto
{
    public class CreateEvaluacionRequestDto
    {
        [Required(ErrorMessage = "Introduzca el id del alumno")]
        public int AlumnoId { get; set; }

        [Required(ErrorMessage = "Introduzca el id del profesor")]
        public int ProfesorId { get; set; }

        [Required(ErrorMessage = "Introduzca la nota")]
        [Range(0, 5, ErrorMessage = "Introduzca una nota entre 0 y 5")]
        public int Nota { get; set; }
    }
}
