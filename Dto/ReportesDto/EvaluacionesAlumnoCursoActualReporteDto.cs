using EscuelaCore.Dto.OtrosDto;

namespace EscuelaCore.Dto.ReportesDto
{
    public class EvaluacionesAlumnoCursoActualReporteDto
    {
        public string Alumno { get; set; }
        public int Año { get; set; }
        public List<EvaluacionAlumnoDto> Evaluaciones { get; set; }
    }
}
