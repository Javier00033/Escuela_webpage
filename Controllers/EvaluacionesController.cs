using EscuelaCore.Data;
using EscuelaCore.Enums;
using EscuelaCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EscuelaCore.Services.Interfaces;
using EscuelaCore.Dto.ControllersDto;
using EscuelaCore.Dto.RequestsDto;

namespace EscuelaCore.Controllers
{

    /// <summary>
    /// Controlador para la gestión de evaluaciones.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EvaluacionesController : ControllerBase
    {
        private readonly EscuelaCoreContext _context;
        private readonly ITrazaService _trazaService;
        private readonly IAlumnoService _alumnoService;

        public EvaluacionesController(
            ITrazaService trazaService,
            IAlumnoService alumnoService,
            EscuelaCoreContext context)
        {
            _trazaService = trazaService;
            _alumnoService = alumnoService;
            _context = context;
        }

        /// <summary>
        /// Crea una nueva evaluación para un alumno en una asignatura.
        /// </summary>
        /// <param name="request">Datos de la evaluación a crear.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpPost]
        [Authorize(Roles = "Profesor")]
        public async Task<ActionResult<EvaluacionDto>> CreateEvaluacion([FromBody] CreateEvaluacionRequestDto request)
        {
            try
            {
                var cursoActual = await _context.GetCursoActualAsync();
                var alumno = await _context.Alumnos
                    .Include(a => a.Aula)
                    .Include(a => a.Matriculas)
                    .ThenInclude(m => m.Curso)
                    .FirstOrDefaultAsync(a => a.Id == request.AlumnoId);
                var profesor = await _context.Profesores
                    .Include(p => p.AulaProfesores)
                    .FirstOrDefaultAsync(p => p.Id == request.ProfesorId);

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        Message = "Datos de entrada incorrectos",
                        Errors = ModelState.Values.SelectMany(err => err.Errors)
                    });
                }

                if (profesor == null || !profesor.Activo)
                {
                    return NotFound(new { message = "Profesor no encontrado o no activo" });
                }

                if (alumno == null || !alumno.Activo)
                {
                    return NotFound(new { message = "Alumno no encontrado o no activo" });
                }

                bool evaluacionCursoActual = await _context.Evaluaciones
                    .AnyAsync(e => e.CursoId == cursoActual.Id && e.Asignatura == profesor.Asignatura && e.AlumnoId == request.AlumnoId);

                if (cursoActual.Activo != true)
                {
                    return Conflict(new { message = "No hay ningun curso activo" });
                }

                if (!alumno.Matriculas.Any(m => m.CursoId == cursoActual.Id))
                {
                    return Conflict(new { message = "El alumno no esta matriculado en el curso actual" });
                }

                var asignaturasDelAlumno = _alumnoService.GetAsignaturasPorCarrera(alumno.Carrera);

                if (!asignaturasDelAlumno.Contains(profesor.Asignatura))
                {
                    return Conflict(new { message = $"El alumno no cursa la asignatura {profesor.Asignatura}. El alumno está en la carrera {alumno.Carrera}" });
                }

                bool profesorAsignadoAlAula = profesor.AulaProfesores
                    .Any(ap => ap.AulaId == alumno.AulaId &&
                               ap.Curso.Activo == true);

                if (!profesorAsignadoAlAula)
                {
                    return Conflict(new
                    {
                        message = $"El profesor no está asignado a impartir {profesor.Asignatura} en el aula del alumno para el curso actual"
                    });
                }

                if (evaluacionCursoActual)
                {
                    return Conflict(new { message = "El alumno ya tiene una evaluación para esa asignatura este curso, elimínela o edítela" });
                }

                if (request.Nota < 0 || request.Nota > 5)
                {
                    return Conflict(new { message = "La nota debe estar entre 0 y 5" });
                }

                var evaluacion = new Evaluacion
                {
                    ProfesorId = request.ProfesorId,
                    AlumnoId = request.AlumnoId,
                    Asignatura = profesor.Asignatura,
                    Nota = request.Nota,
                    CursoId = cursoActual.Id,
                    FechaEvaluacion = DateTime.Now,
                };

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    _context.Evaluaciones.Add(evaluacion);
                    await _context.SaveChangesAsync();

                    await _trazaService.RegistrarTrazaAsync(
                        $"Evaluacion creada: Estudiante {evaluacion.Alumno.Nombre} {evaluacion.Alumno.Apellidos}, " +
                        $" Profesor {evaluacion.Profesor.Nombre} {evaluacion.Profesor.Apellidos}, {evaluacion.Asignatura}, {evaluacion.Nota}",
                        "CreateEvaluacion",
                        User.Identity?.Name ?? "Desconocido");

                    await transaction.CommitAsync();

                    return Ok(new { message = "Evaluacion creada" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine(ex.Message);
                    return StatusCode(500, "Error interno al crear evaluacion");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene las evaluaciones de un alumno en una asignatura específica.
        /// </summary>
        /// <param name="alumnoId">Id del alumno.</param>
        /// <param name="asignatura">Asignatura a consultar.</param>
        /// <param name="pagina"></param>
        /// <param name="cantidadDeRegistrosPorPagina"></param>
        /// <returns>Lista paginada de evaluaciones.</returns>
        [HttpGet("alumno/{alumnoId}/asignatura/{asignatura}")]
        [Authorize(Roles = "Administrador,Profesor,Alumno")]
        public async Task<ActionResult<List<EvaluacionDto>>> GetEvaluacionesAlumnoAsignatura(int alumnoId,
            Asignatura asignatura,
            [FromQuery] int pagina = 1,
            [FromQuery] int cantidadDeRegistrosPorPagina = 5)
        {
            try
            {
                var evaluacionesQuery = _context.Evaluaciones
                    .Include(e => e.Profesor)
                    .Include(e => e.Alumno)
                    .Where(e => e.Alumno.Id == alumnoId && e.Asignatura == asignatura && e.Alumno.Activo)
                    .AsQueryable();
                int totalDeRegistros = await evaluacionesQuery.CountAsync();

                var evaluacionAsignatura = evaluacionesQuery
                    .Skip((pagina - 1) * cantidadDeRegistrosPorPagina)
                    .Take(cantidadDeRegistrosPorPagina)
                    .Select(e => new EvaluacionListaDto
                    {
                        Nota = e.Nota,
                        CursoId = e.Curso.Id,
                        FechaEvaluacion = e.FechaEvaluacion
                    })
                    .ToList();

                if (totalDeRegistros == 0)
                {
                    return Ok($"No se encontraron evaluaciones");
                }

                var model = new Paginacion<EvaluacionListaDto>
                {
                    Datos = evaluacionAsignatura,
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
        /// Actualiza los datos de una evaluación existente.
        /// </summary>
        /// <param name="id">ID de la evaluación a actualizar.</param>
        /// <param name="request">Datos actualizados de la evaluación.</param>
        /// <returns>Datos de la evaluación actualizada.</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Profesor")]
        public async Task<ActionResult<EvaluacionDto>> UpdateNota(int id, [FromBody] UpdateEvaluacionRequestDto request)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "Id de evaluacion incorrecto" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        Message = "Datos de entrada incorrectos",
                        Errors = ModelState.Values.SelectMany(err => err.Errors)
                    });
                }

                var evaluacion = await _context.Evaluaciones
                    .Include(e => e.Alumno)
                    .Include(e => e.Profesor)
                    .Include(e => e.Curso)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (request.Nota < 0 || request.Nota > 5)
                {
                    return Conflict(new { message = "La nota debe estar entre 0 y 5" });
                }

                if (evaluacion == null)
                    return NotFound("Evaluación no encontrada");

                var cursoActual = await _context.GetCursoActualAsync();

                if (evaluacion.CursoId != cursoActual.Id)
                    return Conflict("No se pueden modificar evaluaciones de cursos anteriores.");


                evaluacion.ProfesorId = evaluacion.ProfesorId;
                evaluacion.AlumnoId = evaluacion.AlumnoId;
                evaluacion.Asignatura = evaluacion.Asignatura;
                evaluacion.Nota = request.Nota;
                evaluacion.FechaEvaluacion = DateTime.Now;

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    _context.Evaluaciones.Update(evaluacion);
                    await _context.SaveChangesAsync();

                    await _trazaService.RegistrarTrazaAsync(
                        $"Evaluacion editada: Estudiante {evaluacion.Alumno.Nombre} {evaluacion.Alumno.Apellidos}, " +
                        $" Profesor {evaluacion.Profesor.Nombre} {evaluacion.Profesor.Apellidos}, {evaluacion.Asignatura}, {evaluacion.Nota}",
                        "UpdateNota",
                        User.Identity?.Name ?? "Desconocido");

                    await transaction.CommitAsync();

                    return Ok(new EvaluacionDto
                    {
                        Profesor = $"{evaluacion.Profesor.Nombre} {evaluacion.Profesor.Apellidos}",
                        Alumno = $"{evaluacion.Alumno.Nombre} {evaluacion.Alumno.Apellidos}",
                        Asignatura = evaluacion.Asignatura,
                        Nota = evaluacion.Nota,
                        FechaEvaluacion = evaluacion.FechaEvaluacion
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine(ex.Message);
                    return StatusCode(500, "Error interno al actualizar evaluacion");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Elimina una evaluación existente.
        /// </summary>
        /// <param name="id">ID de la evaluación a eliminar.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Profesor")]
        public async Task<IActionResult> DeleteEvaluacion(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { message = "Id de evaluación incorrecto" });
                }

                var evaluacion = await _context.Evaluaciones
                    .Include(e => e.Alumno)
                    .Include(e => e.Profesor)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (evaluacion == null)
                {
                    return NotFound(new { message = "Evaluación no encontrada" });
                }

                if (evaluacion.FechaEvaluacion.Year < DateTime.Now.Year)
                {
                    return Conflict(new { message = "No se pueden eliminar evaluaciones de cursos anteriores." });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    _context.Evaluaciones.Remove(evaluacion);
                    await _context.SaveChangesAsync();

                    var alumnoNombre = evaluacion.Alumno != null
                        ? $"{evaluacion.Alumno.Nombre} {evaluacion.Alumno.Apellidos}"
                        : "Alumno desconocido";

                    var profesorNombre = evaluacion.Profesor != null
                        ? $"{evaluacion.Profesor.Nombre} {evaluacion.Profesor.Apellidos}"
                        : "Profesor desconocido";

                    await _trazaService.RegistrarTrazaAsync(
                        $"Evaluación eliminada: Estudiante {alumnoNombre}, " +
                        $"Profesor {profesorNombre}, {evaluacion.Asignatura}, {evaluacion.Nota}",
                        "DeleteEvaluacion",
                        User.Identity?.Name ?? "Desconocido");

                    await transaction.CommitAsync();

                    return Ok(new { message = "Evaluación eliminada correctamente" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine(ex.Message);
                    return StatusCode(500, "Error interno al eliminar evaluación");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}