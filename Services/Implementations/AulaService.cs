using EscuelaCore.Data;
using EscuelaCore.Enums;
using EscuelaCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EscuelaCore.Services.Implementations
{
    /// <summary>
    /// Servicio para operaciones relacionadas con aulas.
    /// </summary>
    public class AulaService : IAulaService
    {
        private readonly EscuelaCoreContext _context;

        public AulaService(EscuelaCoreContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Verifica si el aula tiene todos los profesores requeridos para la carrera en el curso actual.
        /// </summary>
        /// <param name="aulaId">ID del aula.</param>
        /// <param name="carrera">Carrera a verificar.</param>
        /// <returns>True si el claustro está completo, false en caso contrario.</returns>
        public async Task<bool> AulaTieneClaustroCompleto(int aulaId, Carrera carrera)
        {
            var cursoActual = await _context.GetCursoActualAsync();

            var asignaturasRequeridas = carrera == Carrera.Ciencias ?
                new[] { Asignatura.Matematicas, Asignatura.Informatica, Asignatura.EducacionFisica } :
                new[] { Asignatura.Español, Asignatura.Historia, Asignatura.EducacionFisica };

            var profesoresAsignados = await _context.AulaProfesores
                .Where(ap => ap.AulaId == aulaId && ap.Curso.Id == cursoActual.Id && ap.Profesor.Activo)
                .Select(ap => ap.Asignatura)
                .ToListAsync();

            return asignaturasRequeridas.All(asignatura => profesoresAsignados.Contains(asignatura));
        }
    }
}
