using EscuelaCore.Enums;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Models
{
    public class Matricula
    {
        public int Id { get; set; }

        [Required]
        public DateTime FechaMatricula { get; set; }  
        public int AlumnoId { get; set; }
        public Alumno Alumno { get; set; }
        public int AulaId { get; set; }
        public Aula Aula { get; set; }
        public Carrera Carrera { get; set; }
        public int CursoId { get; set; }
        public Curso Curso { get; set; }
    }
}
