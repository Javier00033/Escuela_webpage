using EscuelaCore.Enums;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Models
{
    public class Evaluacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AlumnoId { get; set; }
        public Alumno Alumno { get; set; } = null!;

        [Required]
        public int ProfesorId { get; set; }
        public Profesor Profesor { get; set; }

        [Required]
        public Asignatura Asignatura { get; set; }

        [Range(0, 5)]
        public int Nota { get; set; }
        public bool Editable { get; set; } = true;
        public DateTime FechaEvaluacion { get; set; }
        public int CursoId { get; set; }
        public Curso Curso { get; set; }
    }
}
