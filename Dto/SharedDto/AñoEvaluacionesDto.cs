namespace EscuelaCore.Dto.OtrosDto
{
    public class AñoEvaluacionesDto
    {
        public int Año { get; set; }
        public string Carrera { get; set; }
        public List<EvaluacionAlumnoDto> Evaluaciones { get; set; }
    }
}
