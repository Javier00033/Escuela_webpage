using EscuelaCore.Dto.SharedDto;
using EscuelaCore.Enums;
using EscuelaCore.Models;

namespace EscuelaCore.Dto.ControllersDto
{
    public class AulaDto
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public Carrera Carrera { get; set; }
        public int ProfesorAula { get; set; }
        public int AlumnosAula { get; set; }
    }
}
