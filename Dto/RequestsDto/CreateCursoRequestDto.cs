using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Dto.RequestsDto
{
    public class CreateCursoRequestDto
    {
        [Required(ErrorMessage = "Introduzca un nombre para el nuevo curso")]
        [MaxLength(250)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Introduzca la fecha de inicio")]
        [DefaultValue("2025-10-03")]
        public DateTime FechaInicio { get; set; }

        [Required(ErrorMessage = "Introduzca la fecha de finalizacion")]
        [DefaultValue("2025-10-03")]
        public DateTime FechaFin { get; set; }
    }
}
