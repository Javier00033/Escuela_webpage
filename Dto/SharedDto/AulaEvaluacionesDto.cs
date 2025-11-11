namespace EscuelaCore.Dto.OtrosDto
{
    public class AulaEvaluacionesDto
    {
        public int NumeroAula { get; set; }
        public string Carrera { get; set; }
        public List<AlumnoEvaluacionDto> Alumnos { get; set; } = new List<AlumnoEvaluacionDto>();
    }
}
