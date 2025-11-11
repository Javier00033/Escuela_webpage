using EscuelaCore.Enums;
using EscuelaCore.Dto.SharedDto;
using EscuelaCore.Models;

namespace EscuelaCore.Dto.ControllersDto
{
    public class ProfesorDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string CI { get; set; }
        public Asignatura Asignatura { get; set; }
        public List<AulaProfesorDto> AulaProfesores { get; set; }
    }
}
