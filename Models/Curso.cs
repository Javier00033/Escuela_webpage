using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EscuelaCore.Models
{
    public class Curso
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(250)]
        public string Nombre { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        [Required]
        public bool Activo { get; set; } = true;

        public ICollection<Matricula> Matriculas { get; set; }
        public ICollection<Evaluacion> Evaluaciones { get; set; }
        public ICollection<AulaProfesor> AulaProfesores { get; set; }
    }
}