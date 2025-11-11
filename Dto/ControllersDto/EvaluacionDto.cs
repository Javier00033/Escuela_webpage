using EscuelaCore.Enums;
using EscuelaCore.Models;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Dto.ControllersDto
{
    public class EvaluacionDto
    {
        public string Profesor { get; set; }
        public string Alumno { get; set; }
        public Asignatura Asignatura { get; set; }
        public int Nota { get; set; }
        public DateTime FechaEvaluacion { get; set; }
    }
}
