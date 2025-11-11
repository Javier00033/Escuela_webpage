using EscuelaCore.Data;
using EscuelaCore.Dto;
using EscuelaCore.Dto.OtrosDto;
using EscuelaCore.Dto.ReportesDto;
using EscuelaCore.Enums;
using EscuelaCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace EscuelaCore.Controllers
{
    /// <summary>
    /// Controlador para reportes y consultas avanzadas.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportesController : ControllerBase
    {
        private readonly EscuelaCoreContext _context;

        public ReportesController(EscuelaCoreContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene las aulas y alumnos asignados a un profesor en el curso actual.
        /// </summary>
        /// <param name="profesorId">ID del profesor.</param>
        /// <param name="filtroAulasId">IDs de aulas para filtrar (opcional).</param>
        /// <param name="pagina"></param>
        /// <param name="cantidadDeRegistrosPorPagina"></param>
        /// <returns>Lista paginada de aulas y alumnos.</returns>
        [HttpGet("aulas-con-alumnos-profesor")]
        [Authorize(Roles = "Profesor")]
        public async Task<ActionResult<Paginacion<AulasConAlumnosReporteDto>>> GetAulasConAlumnos(
            [FromQuery] int profesorId,
            [FromQuery] int[]? filtroAulasId = null,
            [FromQuery] int pagina = 1,
            [FromQuery] int cantidadDeRegistrosPorPagina = 5)
        {
            try
            {
                var profesor = await _context.Profesores
                    .Where(p => p.Activo)
                    .FirstOrDefaultAsync(p => p.Id == profesorId);

                if (profesor == null)
                    return NotFound(new { Message = "Profesor no encontrado" });

                var cursoActual = await _context.GetCursoActualAsync();

                var query = _context.AulaProfesores
                    .Include(ap => ap.Profesor)
                    .Include(ap => ap.Aula)
                        .ThenInclude(a => a.Alumnos)
                    .Where(ap => ap.ProfesorId == profesorId && ap.Curso.Id == cursoActual.Id && ap.Profesor.Activo);

                if (filtroAulasId != null && filtroAulasId.Length > 0)
                {
                    query = query.Where(ap => filtroAulasId.Contains(ap.AulaId));
                }

                var aulasDelProfesor = await query
                    .GroupBy(ap => new { ap.ProfesorId, ap.Profesor.Nombre, ap.Profesor.Apellidos })
                    .Select(grupo => new AulasConAlumnosReporteDto
                    {
                        Profesor = $"{grupo.Key.Nombre} {grupo.Key.Apellidos}",
                        CursosActuales = grupo.Select(ap => new CursoActualDto
                        {
                            Aula = ap.Aula.Numero,
                            Alumnos = ap.Aula.Alumnos
                                .Where(a => a.Matriculas.Any(m => m.CursoId == cursoActual.Id))
                                .Select(a => $"{a.Nombre} {a.Apellidos}")
                                .ToList()
                        }).ToList()
                    })
                    .ToListAsync();

                int totalDeRegistros = await query.CountAsync();

                if (totalDeRegistros == 0)
                {
                    return Ok("No se encontraron alumnos");
                }

                var model = new Paginacion<AulasConAlumnosReporteDto>
                {
                    Datos = aulasDelProfesor,
                    PaginaActual = pagina,
                    TotalDeRegistros = totalDeRegistros,
                    RegistrosPorPagina = cantidadDeRegistrosPorPagina,
                    TotalDePaginas = (int)Math.Ceiling(totalDeRegistros / (double)cantidadDeRegistrosPorPagina),
                };

                return Ok(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene las evaluaciones por aula y año para un profesor.
        /// </summary>
        /// <param name="profesorId">ID del profesor.</param>
        /// <param name="filtroAños">Año escolar para filtrar.</param>
        /// <param name="filtroAulasId">IDs de aulas para filtrar (opcional).</param>
        /// <param name="pagina"></param>
        /// <param name="cantidadDeRegistrosPorPagina"></param>
        /// <returns>Lista paginada de evaluaciones por aula y año.</returns>
        [HttpGet("evaluaciones-por-año-profesor")]
        [Authorize(Roles = "Profesor")]
        public async Task<ActionResult<Paginacion<EvaluacionesPorAulasPorAñoReporteDto>>> GetEvaluacionesPorAulasPorAño(
            [FromQuery] int profesorId,
            [FromQuery] int[]? filtroAños = null,
            [FromQuery] int[]? filtroAulasId = null,
            [FromQuery] int pagina = 1,
            [FromQuery] int cantidadDeRegistrosPorPagina = 5)
        {
            try
            {
                var profesor = await _context.Profesores
                    .FirstOrDefaultAsync(p => p.Id == profesorId);

                if (profesor == null)
                {
                    return NotFound(new { Message = "Profesor no encontrado" });
                }

                var query = _context.AulaProfesores
                    .Include(ap => ap.Curso)
                    .Include(ap => ap.Aula)
                        .ThenInclude(a => a.Alumnos)
                            .ThenInclude(al => al.Evaluaciones
                                .Where(e => e.ProfesorId == profesorId && e.Asignatura == profesor.Asignatura))
                    .Include(ap => ap.Profesor)
                    .Where(ap => ap.ProfesorId == profesorId);

                if (filtroAulasId != null && filtroAulasId.Length > 0)
                {
                    query = query.Where(ap => filtroAulasId.Contains(ap.AulaId));
                }

                if (filtroAños != null)
                {
                    query = query.Where(ap => filtroAños.Contains(ap.Curso.FechaInicio.Year));
                }

                var aulasDelProfesor = await query.ToListAsync();

                var reporte = new EvaluacionesPorAulasPorAñoReporteDto
                {
                    Profesor = $"{profesor?.Nombre ?? "N/A"} {profesor?.Apellidos ?? "N/A"}",
                    AñosEscolares = aulasDelProfesor
                        .Where(ap => ap != null && ap.Curso != null && ap.Aula != null)
                        .GroupBy(ap => ap.Curso.FechaInicio.Year)
                        .OrderByDescending(g => g.Key)
                        .Select(grupoAño => new AñoEscolarDto
                        {
                            Año = grupoAño.Key,
                            Aulas = grupoAño
                                .Where(ap => ap.Aula != null)
                                .GroupBy(ap => ap.Aula)
                                .Select(grupoAula => new AulaEvaluacionesDto
                                {
                                    NumeroAula = grupoAula.Key?.Numero ?? 0,
                                    Carrera = grupoAula.Key?.Carrera.ToString() ?? "No especificada",
                                    Alumnos = grupoAula.Key?.Alumnos?
                                        .Where(a => a != null && a.Evaluaciones != null && a.Evaluaciones.Any() && a.Activo)
                                        .SelectMany(a => a.Evaluaciones
                                            .Where(e => e != null &&
                                                       e.ProfesorId == profesorId &&
                                                       e.Asignatura == profesor?.Asignatura)
                                            .OrderByDescending(e => e.FechaEvaluacion)
                                            .Take(1)
                                            .Select(e => new AlumnoEvaluacionDto
                                            {
                                                NombreCompleto = $"{a?.Nombre ?? "N/A"} {a?.Apellidos ?? "N/A"}",
                                                Asignatura = e?.Asignatura.ToString() ?? "No especificada",
                                                Calificacion = e?.Nota ?? 0
                                            }))
                                        .Where(alumno => alumno != null)
                                        .OrderBy(a => a.NombreCompleto)
                                        .ToList() ?? new List<AlumnoEvaluacionDto>()
                                })
                                .Where(aula => aula != null && aula.Alumnos != null && aula.Alumnos.Any())
                                .OrderBy(aula => aula.NumeroAula)
                                .ToList()
                        })
                        .Where(año => año != null && año.Aulas != null && año.Aulas.Any())
                        .ToList()
                };

                int totalDeRegistros = reporte.AñosEscolares.Count;

                if (totalDeRegistros == 0)
                {
                    return Ok("No se encontraron resultados para el profesor");
                }

                var model = new Paginacion<EvaluacionesPorAulasPorAñoReporteDto>
                {
                    Datos = new List<EvaluacionesPorAulasPorAñoReporteDto> { reporte },
                    PaginaActual = pagina,
                    TotalDeRegistros = totalDeRegistros,
                    RegistrosPorPagina = cantidadDeRegistrosPorPagina,
                    TotalDePaginas = (int)Math.Ceiling(totalDeRegistros / (double)cantidadDeRegistrosPorPagina),
                };

                return Ok(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene las evaluaciones del alumno en el curso actual.
        /// </summary>
        /// <param name="alumnoId">ID del alumno.</param>
        /// <returns>Evaluaciones del alumno en el curso actual.</returns>
        [HttpGet("evaluaciones-curso-actual")]
        [Authorize(Roles = "Alumno")]
        public async Task<ActionResult<EvaluacionesAlumnoCursoActualReporteDto>> GetEvaluacionesAlumnoCursoActual([FromQuery] int alumnoId)
        {
            try
            {
                var alumno = await _context.Alumnos
                    .Include(a => a.Aula)
                    .FirstOrDefaultAsync(a => a.Id == alumnoId && a.Activo);

                var cursoActual = await _context.GetCursoActualAsync();

                var todasEvaluaciones = await _context.Evaluaciones
                    .Where(e => e.AlumnoId == alumnoId && e.FechaEvaluacion.Year == cursoActual.FechaInicio.Year)
                    .OrderByDescending(e => e.FechaEvaluacion)
                    .ToListAsync();


                if (alumno == null)
                {
                    return NotFound(new { Message = "Alumno no encontrado" });
                }

                var añoActual = cursoActual.FechaInicio.Year;

                var evaluaciones = todasEvaluaciones
                    .GroupBy(e => e.Asignatura)
                    .Select(g => g.First())
                    .Select(e => new EvaluacionAlumnoDto
                    {
                        Asignatura = e.Asignatura.ToString(),
                        Calificacion = e.Nota
                    })
                    .ToList();

                var reporte = new EvaluacionesAlumnoCursoActualReporteDto
                {
                    Alumno = $"{alumno.Nombre} {alumno.Apellidos}",
                    Año = añoActual,
                    Evaluaciones = evaluaciones
                };

                return Ok(reporte);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene las evaluaciones del alumno agrupadas por año.
        /// </summary>
        /// <param name="alumnoId">ID del alumno.</param>
        /// <param name="filtroAños">Años para filtrar (opcional).</param>
        /// <returns>Evaluaciones del alumno por año.</returns>
        [HttpGet("evaluaciones-por-año")]
        [Authorize(Roles = "Alumno")]
        public async Task<ActionResult<EvaluacionesAlumnoPorAñoReporteDto>> GetEvaluacionesAlumnoPorAño(
            [FromQuery] int alumnoId,
            [FromQuery] int[]? filtroAños = null)
        {
            try
            {
                var alumno = await _context.Alumnos
                    .FirstOrDefaultAsync(a => a.Id == alumnoId && a.Activo);

                if (alumno == null)
                {
                    return NotFound(new { Message = "Alumno no encontrado" });
                }

                var todasEvaluaciones = await _context.Evaluaciones
                    .Where(e => e.AlumnoId == alumnoId)
                    .OrderByDescending(e => e.FechaEvaluacion)
                    .ToListAsync();

                if (filtroAños != null && filtroAños.Length > 0)
                {
                    todasEvaluaciones = todasEvaluaciones
                        .Where(e => filtroAños.Contains(e.FechaEvaluacion.Year))
                        .ToList();
                }

                var evaluacionesPorAño = todasEvaluaciones
                            .GroupBy(e => new { Año = e.FechaEvaluacion.Year, Carrera = alumno.Carrera })
                            .Select(g => new
                            {
                                Año = g.Key.Año,
                                Carrera = g.Key.Carrera.ToString(),
                                Evaluaciones = g
                                    .GroupBy(e => e.Asignatura)
                                    .Select(ge => ge.First())
                                    .Select(e => new EvaluacionAlumnoDto
                                    {
                                        Asignatura = e.Asignatura.ToString(),
                                        Calificacion = e.Nota
                                    })
                                    .ToList()
                            })
                            .ToList();

                var reporte = new EvaluacionesAlumnoPorAñoReporteDto
                {
                    Alumno = $"{alumno.Nombre} {alumno.Apellidos}",
                    EvaluacionesPorAño = evaluacionesPorAño
                        .OrderByDescending(e => e.Año)
                        .Select(e => new AñoEvaluacionesDto
                        {
                            Año = e.Año,
                            Carrera = e.Carrera,
                            Evaluaciones = e.Evaluaciones
                        })
                        .ToList()
                };

                return Ok(reporte);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene el reporte de matrículas y bajas por año y carrera.
        /// </summary>
        /// <param name="filtroAños">Años para filtrar (opcional).</param>
        /// <param name="filtroCarreras">Carreras para filtrar (opcional).</param>
        /// <param name="pagina"></param>
        /// <param name="cantidadDeRegistrosPorPagina"></param>
        /// <returns>Reporte de matrículas y bajas por año.</returns>
        [HttpGet("matriculas-bajas")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<Paginacion<AñoMatriculasBajasDto>>> GetMatriculasYBajasPorAño(
            [FromQuery] int[]? filtroAños = null,
            [FromQuery] Carrera[]? filtroCarreras = null,
            [FromQuery] int pagina = 1,
            [FromQuery] int cantidadDeRegistrosPorPagina = 5)
        {
            try
            {
                var todasLasMatriculas = await _context.Matriculas
                    .Include(m => m.Alumno)
                    .Include(m => m.Aula)
                    .Include(m => m.Curso)
                    .ToListAsync();

                var matriculasActivas = todasLasMatriculas
                    .Where(m => m.Alumno.Activo ||
                               (!m.Alumno.Activo && m.Alumno.FechaBaja > m.FechaMatricula))
                    .ToList();

                var matriculasConBaja = todasLasMatriculas
                    .Where(m => !m.Alumno.Activo && m.Alumno.FechaBaja.Year == m.Curso.FechaInicio.Year)
                    .ToList();

                if (filtroAños != null && filtroAños.Length > 0)
                {
                    matriculasActivas = matriculasActivas
                        .Where(m => filtroAños.Contains(m.Curso.FechaInicio.Year))
                        .ToList();

                    matriculasConBaja = matriculasConBaja
                        .Where(m => filtroAños.Contains(m.Curso.FechaInicio.Year))
                        .ToList();
                }

                if (filtroCarreras != null && filtroCarreras.Length > 0)
                {
                    matriculasActivas = matriculasActivas
                        .Where(m => filtroCarreras.Contains(m.Carrera))
                        .ToList();

                    matriculasConBaja = matriculasConBaja
                        .Where(m => filtroCarreras.Contains(m.Carrera))
                        .ToList();
                }

                var matriculasPorAño = matriculasActivas
                    .Where(m => m.Curso != null)
                    .GroupBy(m => new { Año = m.Curso.FechaInicio.Year, m.Carrera })
                    .Select(g => new
                    {
                        Año = g.Key.Año,
                        Carrera = g.Key.Carrera,
                        Matriculas = g.Select(m => new MatriculaBajaDto
                        {
                            Alumno = $"{m.Alumno.Nombre} {m.Alumno.Apellidos}",
                            Fecha = m.FechaMatricula
                        }).OrderBy(m => m.Fecha).ToList()
                    })
                    .ToList();

                var bajasPorAño = matriculasConBaja
                     .Where(m => m.Curso != null)
                     .GroupBy(m => new { Año = m.Curso.FechaInicio.Year, m.Carrera })
                     .Select(g => new
                     {
                         Año = g.Key.Año,
                         Carrera = g.Key.Carrera,
                         Bajas = g.Select(m => new MatriculaBajaDto
                         {
                             Alumno = $"{m.Alumno.Nombre} {m.Alumno.Apellidos}",
                             Fecha = m.Alumno.FechaBaja
                         }).OrderBy(b => b.Fecha).ToList()
                     })
                     .ToList();

                var todosLosAños = matriculasPorAño.Select(m => m.Año)
                    .Concat(bajasPorAño.Select(b => b.Año))
                    .Distinct()
                    .OrderByDescending(a => a)
                    .ToList();

                var añosMatriculasBajas = todosLosAños.Select(año => new AñoMatriculasBajasDto
                {
                    Año = año,
                    Carreras = new List<CarreraMatriculasBajasDto>()
                }).ToList();

                foreach (var añoDto in añosMatriculasBajas)
                {
                    var carrerasDelAño = matriculasPorAño
                        .Where(m => m.Año == añoDto.Año)
                        .Select(m => m.Carrera)
                        .Concat(bajasPorAño.Where(b => b.Año == añoDto.Año)
                            .Select(b => b.Carrera))
                        .Distinct()
                        .ToList();

                    foreach (var carrera in carrerasDelAño)
                    {
                        var matriculasCarrera = matriculasPorAño
                            .FirstOrDefault(m => m.Año == añoDto.Año && m.Carrera == carrera)
                            ?.Matriculas ?? new List<MatriculaBajaDto>();

                        var bajasCarrera = bajasPorAño
                            .FirstOrDefault(b => b.Año == añoDto.Año && b.Carrera == carrera)
                            ?.Bajas ?? new List<MatriculaBajaDto>();

                        if (matriculasCarrera.Any() || bajasCarrera.Any())
                        {
                            añoDto.Carreras.Add(new CarreraMatriculasBajasDto
                            {
                                Carrera = carrera.ToString(),
                                Matriculas = matriculasCarrera,
                                Bajas = bajasCarrera
                            });
                        }
                    }

                    añoDto.Carreras = añoDto.Carreras.OrderBy(c => c.Carrera).ToList();
                }

                añosMatriculasBajas = añosMatriculasBajas
                    .Where(año => año.Carreras.Any())
                    .ToList();

                int totalDeRegistros = añosMatriculasBajas.Count;

                if (totalDeRegistros == 0)
                {
                    return Ok(new { Message = "No se encontraron matrículas o bajas para los filtros aplicados" });
                }

                var datosPaginados = añosMatriculasBajas
                    .Skip((pagina - 1) * cantidadDeRegistrosPorPagina)
                    .Take(cantidadDeRegistrosPorPagina)
                    .ToList();

                var model = new Paginacion<AñoMatriculasBajasDto>
                {
                    Datos = datosPaginados,
                    PaginaActual = pagina,
                    TotalDeRegistros = totalDeRegistros,
                    RegistrosPorPagina = cantidadDeRegistrosPorPagina,
                    TotalDePaginas = (int)Math.Ceiling(totalDeRegistros / (double)cantidadDeRegistrosPorPagina),
                };

                return Ok(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}
