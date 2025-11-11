using EscuelaCore.Dto.SharedDto;
using EscuelaCore.Enums;

namespace EscuelaCore.Dto.ControllersDto
{
    public class AulaDetallesDto
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public Carrera Carrera { get; set; }
        public required List<ProfesorAulaDto> ProfesorAulas { get; set; }
        public required List<MatriculaAulaDto> Matriculas { get; set; }
    }
}
