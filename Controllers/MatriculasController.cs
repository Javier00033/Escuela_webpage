using EscuelaCore.Data;
using EscuelaCore.Dto.ControllersDto;
using EscuelaCore.Dto.RequestsDto;
using EscuelaCore.Enums;
using EscuelaCore.Models;
using EscuelaCore.Services.Implementations;
using EscuelaCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EscuelaCore.Controllers
{

    /// <summary>
    /// Controlador para la gestión de matrículas.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class MatriculasController : ControllerBase
    {
        private readonly EscuelaCoreContext _context;
        private readonly IAlumnoService _alumnoService;
        private readonly ITrazaService _trazaService;
        private readonly IAulaService _aulaService;

        public MatriculasController(
            IAulaService aulaService,
            ITrazaService trazaService,
            IAlumnoService alumnoService,
            EscuelaCoreContext context)
        {
            _aulaService = aulaService;
            _trazaService = trazaService;
            _alumnoService = alumnoService;
            _context = context;
        }

        /// <summary>
        /// Crea una nueva matrícula para un alumno en un aula y carrera.
        /// </summary>
        /// <param name="request">Datos de la matrícula a crear.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpPost]
        public async Task<ActionResult> CreateMatricula([FromBody] CreateMatriculaRequestDto request)
        {
            try
            {
                var mesActual = DateTime.Now.Month;
                //if (mesActual != 7 && mesActual != 8)
                //    return Conflict("Las matrículas solo pueden realizarse en julio y agosto");

                var alumno = await _context.Alumnos
                    .Include(a => a.Matriculas)
                    .ThenInclude(m => m.Curso)
                    .Include(a => a.Evaluaciones)
                    .FirstOrDefaultAsync(a => a.Id == request.AlumnoId);

                var claustroCompleto = await _aulaService
                    .AulaTieneClaustroCompleto(request.AulaId, request.Carrera);

                var aula = await _context.Aulas
                    .Include(a => a.Alumnos)
                    .Include(a => a.AulaProfesores)
                    .FirstOrDefaultAsync(a => a.Id == request.AulaId);

                var cursoActual = await _context.GetCursoActualAsync();

                if (alumno == null)
                    return NotFound("Alumno no encontrado");

                if (aula == null)
                    return NotFound("Aula no encontrada");

                if (alumno.Matriculas.Any(m => m.CursoId == cursoActual.Id))
                {
                    return Conflict(new { Message = "Ya el alumno tiene una matricula ectiva en el curso actual" });
                }

                if (aula.Carrera != request.Carrera)
                {
                    return Conflict(new { Message = "La carrera del alumno debe coincidir con la carrera del aula." });
                }

                if (aula.Alumnos.Count(a => a.Activo) >= 5)
                    return Conflict("El aula ya está llena (máximo 5 alumnos)");

                if (claustroCompleto == false)
                {
                    return Conflict(new { Message = "El aula no tiene un claustro de profesores completo para la carrera seleccionada." });
                }

                if (!alumno.Activo)
                    return Conflict("El alumno está dado de baja, no puede matricularse más en el centro");

                if (alumno.Matriculas.Count >= 3)
                    return Conflict("El alumno ha alcanzado el límite de 3 matrículas");

                var asignaturasRequeridas = request.Carrera == Carrera.Ciencias
                    ? new[] { Asignatura.Matematicas, Asignatura.Informatica, Asignatura.EducacionFisica }
                    : new[] { Asignatura.Español, Asignatura.Historia, Asignatura.EducacionFisica };

                var añoAnterior = (DateTime.Now.Year - 1).ToString();
                var evaluacionesAnteriores = await _context.Evaluaciones
                    .Where(e => e.AlumnoId == alumno.Id && e.FechaEvaluacion.Year.ToString() == añoAnterior)
                    .Select(e => e.Asignatura)
                    .ToListAsync();

                if (evaluacionesAnteriores.Count > 0 && asignaturasRequeridas.Except(evaluacionesAnteriores).Any())
                    return Conflict("No se puede iniciar nuevo curso si hay asignaturas sin evaluar del año anterior.");

                bool puedeMatricularse = await _alumnoService.PuedeMatricularseEnCarrera(request.AlumnoId, request.Carrera);

                if (!puedeMatricularse)
                {
                    var carreraAprobada = await _alumnoService.ObtenerCarreraAprobada(request.AlumnoId);
                    return Conflict(new
                    {
                        Message = $"El alumno ya aprobó la carrera {carreraAprobada}. " + $"Solo puede matricularse en la otra carrera."
                    });
                }

                var profesoresAsignados = aula.AulaProfesores
                    .Where(ap => ap.Curso.Activo == true)
                    .Select(ap => ap.Asignatura)
                    .ToList();

                var matricula = new Matricula
                {
                    AlumnoId = request.AlumnoId,
                    AulaId = request.AulaId,
                    Carrera = request.Carrera,
                    FechaMatricula = DateTime.Now,
                    CursoId = cursoActual.Id
                };

                alumno.AulaId = request.AulaId;
                alumno.Carrera = request.Carrera;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Matriculas.Add(matricula);
                    await _context.SaveChangesAsync();

                    await _trazaService.RegistrarTrazaAsync(
                        $"Matrícula creada: Alumno {alumno.Nombre} {alumno.Apellidos}, Aula {aula.Numero}, Carrera {request.Carrera}",
                        "CreateMatricula",
                        User.Identity?.Name ?? "Desconocido");

                    await transaction.CommitAsync();

                    return Ok(new { message = "Matrícula creada exitosamente" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine(ex.Message);
                    return StatusCode(500, "Error al intentar crear matricula");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Actualiza una matrícula existente de un alumno.
        /// </summary>
        /// <param name="id">Id de la matrícula a actualizar.</param>
        /// <param name="request">Datos actualizados de la matrícula.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateMatricula(int id, [FromBody] UpdateMatriculaRequestDto request)
        {
            try
            {
                var mesActual = DateTime.Now.Month;
                if (mesActual != 7 && mesActual != 8)
                    return Conflict("Las matrículas solo pueden modificarse en julio y agosto");

                if (!ModelState.IsValid)
                    return BadRequest(new
                    {
                        Message = "Datos de entrada inválidos",
                        Errors = ModelState.Values.SelectMany(err => err.Errors)
                    });

                var matricula = await _context.Matriculas
                    .Include(m => m.Alumno)
                    .Include(m => m.Aula)
                    .Include(m => m.Curso)
                    .FirstOrDefaultAsync(m => m.Id == id);

                var aula = await _context.Aulas
                    .Include(a => a.Alumnos)
                    .Include(a => a.AulaProfesores)
                    .FirstOrDefaultAsync(a => a.Id == request.AulaId);

                if (matricula == null)
                    return NotFound(new { Message = "Matrícula no encontrada" });

                var alumno = matricula.Alumno;

                if (alumno == null)
                    return NotFound("Alumno no encontrado");

                if (!alumno.Activo)
                    return Conflict(new { Message = "El alumno está dado de baja, no puede modificar matrícula" });

                if (aula == null)
                    return NotFound(new { Message = "Aula no encontrada" });

                if (aula.Carrera != request.Carrera)
                {
                    return Conflict(new { Message = "La carrera del alumno debe coincidir con la carrera del aula." });
                }

                if (request.AulaId != matricula.AulaId)
                {
                    if (aula.Alumnos.Count(a => a.Activo) >= 5)
                        return Conflict(new { Message = "El aula ya está llena (máximo 5 alumnos)" });
                }

                var claustroCompleto = await _aulaService
                    .AulaTieneClaustroCompleto(request.AulaId, request.Carrera);

                if (claustroCompleto == false)
                {
                    return Conflict(new { Message = "El aula no tiene un claustro de profesores completo para la carrera seleccionada." });
                }

                var cursoActual = await _context.GetCursoActualAsync();

                matricula.AulaId = request.AulaId;
                matricula.FechaMatricula = DateTime.Now;
                matricula.CursoId = cursoActual.Id;
                alumno.AulaId = request.AulaId;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Matriculas.Update(matricula);
                    _context.Alumnos.Update(alumno);
                    await _context.SaveChangesAsync();

                    await _trazaService.RegistrarTrazaAsync(
                        $"Matrícula actualizada: Alumno {alumno.Nombre} {alumno.Apellidos}, " +
                        $"Aula {aula.Numero}, Carrera {request.Carrera}",
                        "UpdateMatricula",
                        User.Identity?.Name ?? "Desconocido");

                    await transaction.CommitAsync();

                    return Ok(new { message = "Matrícula actualizada exitosamente" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine(ex.Message);
                    return StatusCode(500, "Error al intentar actualizar matrícula");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtiene las matrículas de un alumno por su id.
        /// </summary>
        /// <param name="id">ID del alumno.</param>
        /// <param name="pagina"></param>
        /// <param name="cantidadDeRegistrosPorPagina"></param>
        /// <returns>Lista paginada de matrículas del alumno.</returns>
        [HttpGet("alumno/{id}")]
        public async Task<ActionResult<List<MatriculaListaDto>>> GetMatriculasAlumno(int id,
            [FromQuery] int pagina = 1,
            [FromQuery] int cantidadDeRegistrosPorPagina = 5)
        {
            try
            {
                var matriculasQuery = _context.Matriculas.AsQueryable();
                int totalDeRegistros = await matriculasQuery.CountAsync();

                var matriculas = matriculasQuery.Where(m => m.Alumno.Id == id && m.Alumno.Activo)
                    .Skip((pagina - 1) * cantidadDeRegistrosPorPagina)
                    .Take(cantidadDeRegistrosPorPagina)
                    .Select(m => new MatriculaListaDto
                    {
                        Id = m.Id,
                        AulaId = m.AulaId,
                        Carrera = m.Carrera,
                        Fecha = m.FechaMatricula,
                    })
                    .ToList();

                if (totalDeRegistros == 0)
                {
                    return Ok("No se encontraron resultados");
                }

                var model = new Paginacion<MatriculaListaDto>
                {
                    Datos = matriculas,
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