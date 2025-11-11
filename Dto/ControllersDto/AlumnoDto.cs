using EscuelaCore.Enums;
using EscuelaCore.Models;

namespace EscuelaCore.Dto.ControllersDto
{
    public class AlumnoDto
    {
        public int Id { get; set; }
        public required string Nombre { get; set; }
        public required string CI { get; set; }
    }
}
