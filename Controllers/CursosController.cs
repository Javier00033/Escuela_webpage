using EscuelaCore.Data;
using EscuelaCore.Dto.ControllersDto;
using EscuelaCore.Dto.RequestsDto;
using EscuelaCore.Enums;
using EscuelaCore.Models;
using EscuelaCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EscuelaCore.Controllers
{
    /// <summary>
    /// Controlador para la gestión de cursos escolares.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class CursosController : ControllerBase
    {
        private readonly EscuelaCoreContext _context;
        private readonly ITrazaService _trazaService;

        public CursosController(EscuelaCoreContext context, ITrazaService trazaService)
        {
            _context = context;
            _trazaService = trazaService;
        }

        /// <summary>
        /// Crea un nuevo curso escolar.
        /// </summary>
        /// <param name="request">Datos del curso a crear.</param>
        /// <returns>Datos del curso creado.</returns>
        [HttpPost]
        public async Task<ActionResult<CursoDto>> CreateCurso([FromBody] CreateCursoRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { Message = "Datos de entrada inválidos", Errors = ModelState.Values.SelectMany(err => err.Errors) });

                bool existeSuperposicion = await _context.Cursos
                    .Where(c => c.Activo)
                    .AnyAsync(c =>
                    (request.FechaInicio >= c.FechaInicio && request.FechaInicio <= c.FechaFin) ||
                    (request.FechaFin >= c.FechaInicio && request.FechaFin <= c.FechaFin) ||
                    (request.FechaInicio <= c.FechaInicio && request.FechaFin >= c.FechaFin) ||
                    (request.FechaInicio >= c.FechaInicio && request.FechaFin <= c.FechaFin) ||
                    (request.FechaInicio == c.FechaFin || request.FechaFin == c.FechaInicio));

                if (request.FechaFin <= request.FechaInicio)
                    return Conflict(new { Message = "La fecha de fin de curso es mayor que la fecha de inicio de curso" });

                if ((request.FechaFin - request.FechaInicio).TotalDays < 1)
                    return Conflict(new { Message = "El curso debe tener una duración mínima de 1 día" });

                if (request.FechaInicio <= DateTime.Now || request.FechaFin <= DateTime.Now)
                    return Conflict(new { Message = "No se puede crear cursos que comiencen o terminen en fechas pasadas" });

                if (request.FechaFin.Year > (request.FechaInicio.Year + 2))
                    return Conflict(new { Message = "La fecha final del curso esta muy lejana" });

                if (existeSuperposicion)
                    return Conflict(new { Message = "Ya existe un curso en las fechas seleccionadas" });

                var cursoActual = await _context.GetCursoActualAsync();

                if (cursoActual != null && cursoActual.Activo == true)
                {
                    return Conflict(new
                    {
                        Message = $"No se puede iniciar un nuevo curso porque hay un curso activo actualmente, debe finalizarlo antes de iniciar otro.",
                    });
                }

                var curso = new Curso
                {
                    Nombre = request.Nombre,
                    FechaInicio = request.FechaInicio,
                    FechaFin = request.FechaFin,
                    Activo = true
                };

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    _context.Cursos.Add(curso);
                    await _context.SaveChangesAsync();

                    var aulaProfesoresDelCursoAnterior = await _context.AulaProfesores
                        .Where(ap => ap.Profesor.Activo)
                        .ToListAsync();

                    foreach (var aulaProfesor in aulaProfesoresDelCursoAnterior)
                    {
                        aulaProfesor.CursoId = curso.Id;
                    }

                    await _context.SaveChangesAsync();
                    

                    await _context.SaveChangesAsync();
                    await _trazaService.RegistrarTrazaAsync(
                        $"Curso creado: {curso.Nombre} ({curso.FechaInicio:yyyy} - {curso.FechaFin:yyyy})",
                        "CreateCurso",
                        User.Identity?.Name ?? "Desconocido");

                    await transaction.CommitAsync();

                    return CreatedAtAction(nameof(GetCurso), new { id = curso.Id }, new CursoDto
                    {
                        Id = curso.Id,
                        Nombre = curso.Nombre,
                        FechaInicio = curso.FechaInicio,
                        FechaFin = curso.FechaFin,
                        Activo = curso.Activo
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine(ex.Message);
                    return StatusCode(500, "Error interno al crear curso");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Finaliza un curso escolar existente.
        /// </summary>
        /// <param name="id">Id del curso a finalizar.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpPost("{id}/finalizar")]
        public async Task<IActionResult> FinalizarCurso(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var curso = await _context.Cursos
                    .Include(c => c.Matriculas)
                    .ThenInclude(m => m.Alumno)
                    .ThenInclude(a => a.Evaluaciones)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (curso == null) return NotFound("Curso no encontrado");
                if (!curso.Activo) return Conflict("El curso ya está finalizado");
                //if (DateTime.Now < curso.FechaFin)
                //{
                //    return Conflict(new
                //    {
                //        Message = "No se puede finalizar el curso antes de la fecha de fin planeada",
                //        FechaFinPlaneada = curso.FechaFin
                //    });
                //}

                var alumnosSinEvaluar = await _context.Matriculas
                .Where(m => m.CursoId == curso.Id && m.Alumno.Activo)
                .Select(m => new {
                    m.AlumnoId,
                    m.Alumno.Carrera,
                    EvaluacionesCount = m.Alumno.Evaluaciones.Count(e => e.CursoId == curso.Id)
                })
                .ToListAsync();

                var alumnosSinEvaluacionCompleta = alumnosSinEvaluar
                    .Where(e =>
                        (e.Carrera == Carrera.Ciencias ? 3 : 3) > e.EvaluacionesCount
                    )
                    .ToList();

                if (alumnosSinEvaluacionCompleta.Any())
                {
                    var alumnosId = alumnosSinEvaluacionCompleta
                        .Select(a => $"ID: {a.AlumnoId}, {a.Carrera}")
                        .ToList();

                    return Conflict(new
                    {
                        Message = $"No se puede finalizar el curso porque existen alumnos sin evaluaciones completas.",
                        AlumnosSinEvaluar = alumnosId,
                        TotalAlumnos = alumnosSinEvaluacionCompleta.Count
                    });
                }           

                curso.Activo = false;
                curso.FechaFin = DateTime.Now;

                _context.Cursos.Update(curso);
                await _context.SaveChangesAsync();

                await _trazaService.RegistrarTrazaAsync(
                    $"Curso finalizado: {curso.Nombre}",
                    "FinalizarCurso",
                    User.Identity?.Name ?? "Desconocido");

                await transaction.CommitAsync();

                return Ok(new { message = "Curso finalizado correctamente" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno al finalizar curso");
            }
        }

        /// <summary>
        /// Obtiene los detalles de un curso por su id.
        /// </summary>
        /// <param name="id">Id del curso.</param>
        /// <returns>Datos del curso.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<CursoDto>> GetCurso(int id)
        {
            try
            {
                var curso = await _context.Cursos
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (curso == null) return NotFound("Curso no encontrado");
                if (curso.FechaInicio.Year < 2024) return BadRequest("Año de inicio de curso incorrecto");

                return Ok(new CursoDto
                {
                    Id = curso.Id,
                    Nombre = curso.Nombre,
                    FechaInicio = curso.FechaInicio,
                    FechaFin = curso.FechaFin,
                    Activo = curso.Activo
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene la lista paginada de cursos escolares.
        /// </summary>
        /// <returns>Lista paginada de cursos.</returns>
        [HttpGet]
        public async Task<ActionResult<Paginacion<CursoDto>>> GetCursos(
        [FromQuery] int pagina = 1,
        [FromQuery] int cantidadDeRegistrosPorPagina = 5)
        {
            try
            {
                var cursosQuery = _context.Cursos.AsQueryable();
                int totalDeRegistros = await cursosQuery.CountAsync();

                var cursos = await cursosQuery
                    .Skip((pagina - 1) * cantidadDeRegistrosPorPagina)
                    .Take(cantidadDeRegistrosPorPagina)
                    .Select(c => new CursoDto
                    {
                        Id = c.Id,
                        Nombre = c.Nombre,
                        FechaInicio = c.FechaInicio,
                        FechaFin = c.FechaFin,
                        Activo = c.Activo
                    })
                    .ToListAsync();

                var model = new Paginacion<CursoDto>
                {
                    Datos = cursos,
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
