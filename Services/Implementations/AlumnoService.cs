using EscuelaCore.Data;
using EscuelaCore.Enums;
using EscuelaCore.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EscuelaCore.Services.Implementations
{
    /// <summary>
    /// Servicio para operaciones relacionadas con alumnos.
    /// </summary>
    public class AlumnoService : IAlumnoService
    {
        private readonly EscuelaCoreContext _context;

        public AlumnoService(EscuelaCoreContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Verifica si el alumno aprobó todas las asignaturas requeridas de la carrera en cualquier curso.
        /// </summary>
        public async Task<bool> AlumnoAproboCarrera(int alumnoId, Carrera carrera)
        {
            var asignaturasRequeridas = GetAsignaturasPorCarrera(carrera);

            var cursosConCarrera = await _context.Matriculas
                .Where(m => m.AlumnoId == alumnoId && m.Carrera == carrera)
                .Select(m => m.CursoId)
                .Distinct()
                .ToListAsync();

            foreach (var cursoId in cursosConCarrera)
            {
                var aproboEnEsteCurso = await AproboCarreraEnCurso(alumnoId, carrera, cursoId);
                if (aproboEnEsteCurso)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Obtiene las asignaturas por carrera.
        /// </summary>
        public List<Asignatura> GetAsignaturasPorCarrera(Carrera carrera)
        {
            return carrera switch
            {
                Carrera.Ciencias => new List<Asignatura>
                {
                    Asignatura.Matematicas,
                    Asignatura.Informatica,
                    Asignatura.EducacionFisica
                },
                Carrera.Letras => new List<Asignatura>
                {
                    Asignatura.Español,
                    Asignatura.Historia,
                    Asignatura.EducacionFisica
                },
                _ => throw new ArgumentException("Carrera no válida")
            };
        }

        /// <summary>
        /// Obtiene qué carrera tiene aprobada el alumno (null si ninguna)
        /// </summary>
        public async Task<Carrera?> ObtenerCarreraAprobada(int alumnoId)
        {
            if (await AlumnoAproboCarrera(alumnoId, Carrera.Ciencias))
                return Carrera.Ciencias;

            if (await AlumnoAproboCarrera(alumnoId, Carrera.Letras))
                return Carrera.Letras;

            return null;
        }

        /// <summary>
        /// Verifica si el alumno tiene alguna carrera aprobada
        /// </summary>
        public async Task<bool> AlumnoTieneCarreraAprobada(int alumnoId)
        {
            return await AlumnoAproboCarrera(alumnoId, Carrera.Ciencias) ||
                   await AlumnoAproboCarrera(alumnoId, Carrera.Letras);
        }

        /// <summary>
        /// Verifica si el alumno puede matricularse en la carrera solicitada
        /// </summary>
        public async Task<bool> PuedeMatricularseEnCarrera(int alumnoId, Carrera carreraSolicitada)
        {
            var carreraAprobada = await ObtenerCarreraAprobada(alumnoId);

            if (carreraAprobada == null)
                return true;

            return carreraSolicitada != carreraAprobada.Value;
        }

        private async Task<bool> AproboCarreraEnCurso(int alumnoId, Carrera carrera, int cursoId)
        {
            var asignaturasRequeridas = GetAsignaturasPorCarrera(carrera);

            var aprobacionPorAsignatura = await _context.Evaluaciones
                .Where(e => e.AlumnoId == alumnoId &&
                           asignaturasRequeridas.Contains(e.Asignatura) &&
                           e.CursoId == cursoId)
                .GroupBy(e => e.Asignatura)
                .Select(g => new
                {
                    Asignatura = g.Key,
                    UltimaNota = g.OrderByDescending(e => e.FechaEvaluacion)
                                  .Select(e => e.Nota)
                                  .FirstOrDefault()
                })
                .ToListAsync();

            return asignaturasRequeridas.All(asignatura =>
                aprobacionPorAsignatura.Any(a =>
                    a.Asignatura == asignatura && a.UltimaNota >= 3));
        }
    }
}