using EscuelaCore.Dto.OtrosDto;

namespace EscuelaCore.Dto.ReportesDto
{
    public class EvaluacionesPorAulasPorAñoReporteDto
    {
        public string Profesor { get; set; }
        public List<AñoEscolarDto> AñosEscolares { get; set; } = new List<AñoEscolarDto>();
    }
}
