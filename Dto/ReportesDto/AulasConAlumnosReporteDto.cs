using EscuelaCore.Dto.OtrosDto;

namespace EscuelaCore.Dto.ReportesDto
{
    public class AulasConAlumnosReporteDto
    {
        public string Profesor { get; set; }
        public List<CursoActualDto> CursosActuales { get; set; }
    }
}
