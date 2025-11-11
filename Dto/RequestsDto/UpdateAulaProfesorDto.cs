using EscuelaCore.Enums;
using System.Text.Json.Serialization;

namespace EscuelaCore.Dto.RequestsDto
{
    public class UpdateAulaProfesorDto
    {
        public int ProfesorId { get; set; }

        [JsonIgnore]
        public Asignatura Asignatura { get; set; }

        [JsonIgnore]
        public int CursoId { get; set; }
    }
}
