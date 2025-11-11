using EscuelaCore.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EscuelaCore.Models
{
    public class Aula
    {
        [Key]
        public int Id { get; set; }

        [Required, Range(0, 10)]
        public int Numero { get; set; }

        [Required]
        public Carrera Carrera { get; set; }

        [NotMapped, Range(0, 5)]
        public int Capacidad { get; set; } = 5;
        public virtual ICollection<Alumno> Alumnos { get; set; }
        public virtual ICollection<Matricula> Matriculas { get; set; }
        public virtual ICollection<AulaProfesor> AulaProfesores { get; set; }
    }
}
