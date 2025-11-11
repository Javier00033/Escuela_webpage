namespace EscuelaCore.Dto.OtrosDto
{
    public class AñoEscolarDto
    {
        public int Año { get; set; }
        public List<AulaEvaluacionesDto> Aulas { get; set; } = new List<AulaEvaluacionesDto>();
    }
}
