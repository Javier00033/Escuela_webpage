using EscuelaCore.Enums;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Dto.RequestsDto
{
    public class UpdateEvaluacionRequestDto
    {
        [Required(ErrorMessage = "Introduzca la nota")]
        [Range(0, 5, ErrorMessage = "Introduzca una nota entre 0 y 5")]
        public int Nota { get; set; }
    }
}
