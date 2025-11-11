using EscuelaCore.Dto.OtrosDto;

namespace EscuelaCore.Dto.ReportesDto
{
    public class EvaluacionesAlumnoPorAñoReporteDto
    {
        public string Alumno { get; set; }
        public List<AñoEvaluacionesDto> EvaluacionesPorAño { get; set; }
    }
}
