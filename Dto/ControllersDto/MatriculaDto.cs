using EscuelaCore.Enums;

namespace EscuelaCore.Dto.ControllersDto
{
    public class MatriculaDto
    {
        public int Id { get; set; }
        public required string CI { get; set; }
        public required string Alumno { get; set; }
        public DateTime Fecha { get; set; }
        public int AulaId { get; set; }
        public Carrera Carrera { get; set; }
    }
}
