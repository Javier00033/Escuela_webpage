namespace EscuelaCore.Dto.OtrosDto
{
    public class AlumnoEvaluacionDto
    {
        public string NombreCompleto { get; set; }
        public string Asignatura { get; set; }
        public decimal Calificacion { get; set; }
        public string Estado => Calificacion >= 3 ? "Aprobado" : "Reprobado";
    }
}
