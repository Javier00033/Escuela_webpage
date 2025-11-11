using EscuelaCore.Enums;

namespace EscuelaCore.Dto.ControllersDto
{
    public class MatriculaListaDto
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public int AulaId { get; set; }
        public Carrera Carrera { get; set; }
    }
}
