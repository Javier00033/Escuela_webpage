using EscuelaCore.Enums;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Models
{
    public class AulaProfesor
    {
        [Key]
        public int Id { get; set; }
        public Asignatura Asignatura { get; set; }
        public int AulaId { get; set; }
        public virtual Aula Aula { get; set; }
        public int ProfesorId { get; set; }
        public virtual Profesor Profesor { get; set; }
        public int CursoId { get; set; }
        public virtual Curso Curso { get; set; }
    }
}
