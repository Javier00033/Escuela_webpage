using EscuelaCore.Enums;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Models
{
    public class Profesor
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(250)]
        [RegularExpression(@"^([A-ZÁÉÍÓÚÑ][a-záéíóúñü'-]*)(\s[A-ZÁÉÍÓÚÑ][a-záéíóúñü'-]*)*$", ErrorMessage = "Cada palabra debe comenzar con mayúscula y solo contener letras válidas")]
        public string Nombre { get; set; }

        [Required, MaxLength(250)]
        [RegularExpression(@"^([A-ZÁÉÍÓÚÑ][a-záéíóúñü'-]*)(\s[A-ZÁÉÍÓÚÑ][a-záéíóúñü'-]*)*$", ErrorMessage = "Cada palabra debe comenzar con mayúscula y solo contener letras válidas")]
        public string Apellidos { get; set;}

        [Required]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "El CI debe tener exactamente 11 dígitos")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "El CI solo puede contener números")]
        public string CI { get; set; }

        [Required]
        public Asignatura Asignatura { get; set; }
        public string UsuarioId { get; set; }
        public Usuario Usuario { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaBaja { get; set; }
        public string NombreCompleto => $"{Nombre} {Apellidos}";
        public DateTime FechaRestauracion { get; set; }
        public virtual ICollection<AulaProfesor> AulaProfesores { get; set; }
        public virtual ICollection<Evaluacion> Evaluaciones { get; set; }
    }
}
