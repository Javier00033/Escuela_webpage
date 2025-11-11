namespace EscuelaCore.Dto.OtrosDto
{
    public class CarreraMatriculasBajasDto
    {
        public string Carrera { get; set; }
        public List<MatriculaBajaDto> Matriculas { get; set; }
        public List<MatriculaBajaDto> Bajas { get; set; }
    }
}
