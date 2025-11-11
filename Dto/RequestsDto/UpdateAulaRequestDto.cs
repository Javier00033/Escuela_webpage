using EscuelaCore.Dto.SharedDto;
using EscuelaCore.Enums;
using EscuelaCore.Models;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Dto.RequestsDto
{
    public class UpdateAulaRequestDto
    {
        [Required(ErrorMessage = "Introduzca la carrera")]
        [EnumDataType(typeof(Carrera), ErrorMessage = "La carrera no es válida")]
        public Carrera Carrera { get; set; }

        [Required(ErrorMessage = "Introduzca el(los) profesor(es)")]
        public List<UpdateAulaProfesorDto> ProfesorAulas { get; set; }
    }
}
