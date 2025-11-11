using EscuelaCore.Data;
using EscuelaCore.Dto.ControllersDto;
using EscuelaCore.Dto.RequestsDto;
using EscuelaCore.Dto.SharedDto;
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
    /// Controlador para la gestión de aulas.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrador")]
    public class AulasController : ControllerBase
    {
        private readonly EscuelaCoreContext _context;
        private readonly ITrazaService _trazaService;

        public AulasController(
            ITrazaService trazaService,
            EscuelaCoreContext context)
        {
            _trazaService = trazaService;
            _context = context;
        }

        /// <summary>
        /// Obtiene la lista paginada de aulas.
        /// </summary>
        /// <returns>Lista paginada de aulas.</returns>
        [HttpGet]
        public async Task<ActionResult<List<AulaDto>>> GetAulas(
        [FromQuery] int pagina = 1,
        [FromQuery] int cantidadDeRegistrosPorPagina = 5)
        {
            try
            {
                var cursoActual = await _context.GetCursoActualAsync();

                var aulasQuery = _context.Aulas
                    .Include(a => a.AulaProfesores)
                    .Include(a => a.Matriculas)
                    .AsQueryable();
                int totalDeRegistros = await aulasQuery.CountAsync();

                var aulas = aulasQuery
                    .Skip((pagina - 1) * cantidadDeRegistrosPorPagina)
                    .Take(cantidadDeRegistrosPorPagina)
                    .Select(a => new AulaDto
                    {
                        Id = a.Id,
                        Numero = a.Numero,
                        Carrera = a.Carrera,
                        ProfesorAula = a.AulaProfesores.Count(),
                        AlumnosAula = a.Matriculas.Where(m => m.Alumno.Activo == true && m.CursoId == cursoActual.Id).Count(),
                    })
                    .ToList();

                if (totalDeRegistros == 0)
                {
                    return NotFound("No se encontraron aulas");
                }

                var model = new Paginacion<AulaDto>
                {
                    Datos = aulas,
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
        /// Obtiene los detalles de un aula por su id.
        /// </summary>
        /// <param name="id">Id del aula.</param>
        /// <returns>Datos del aula.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<AulaDetallesDto>> GetAula(int id)
        {
            try
            {
                var cursoActual = await _context.GetCursoActualAsync();

                if (id <= 0 || id > 10)
                {
                    return BadRequest("id de aula incorrecto");
                }

                var aula = await _context.Aulas
                    .Include(a => a.AulaProfesores)
                        .ThenInclude(ap => ap.Profesor)
                    .Include(a => a.Matriculas.Where(m => m.Alumno.Activo == true && m.CursoId == cursoActual.Id))
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (aula == null)
                {
                    return NotFound($"No se encontró el aula con id {id}");
                }

                var model = new AulaDetallesDto
                {
                    Id = aula.Id,
                    Numero = aula.Numero,
                    Carrera = aula.Carrera,
                    ProfesorAulas = aula.AulaProfesores.Select(ap => new ProfesorAulaDto
                    {
                        ProfesorId = ap.Profesor.Id
                    }).ToList(),

                    Matriculas = aula.Matriculas.Select(m => new MatriculaAulaDto
                    {
                        alumnoId = m.AlumnoId
                    }).ToList()
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
        /// Actualiza los datos de un aula existente.
        /// </summary>
        /// <param name="id">Id del aula a actualizar.</param>
        /// <param name="request">Datos actualizados del aula.</param>
        /// <returns>Datos del aula actualizada.</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<AulaDetallesDto>> UpdateAula(int id, [FromBody] UpdateAulaRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        Message = "Datos de entrada inválidos",
                        Errors = ModelState.Values.SelectMany(err => err.Errors)
                    });
                }

                var aula = await _context.Aulas
                    .Include(a => a.AulaProfesores)
                        .ThenInclude(ap => ap.Profesor)
                    .Include(a => a.Alumnos)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (aula == null)
                {
                    return NotFound(new { Message = "Aula no encontrada" });
                }

                var profesorIds = request.ProfesorAulas.Select(pa => pa.ProfesorId).Distinct();
                var profesores = await _context.Profesores
                    .Where(p => profesorIds.Contains(p.Id) && p.Activo)
                    .ToDictionaryAsync(p => p.Id, p => p.Asignatura);

                if (aula.Carrera != request.Carrera && (aula.Alumnos.Any(a => a.Activo) || aula.AulaProfesores.Any(ap => ap.Profesor.Activo)))
                {
                    return Conflict(new { message = "No se puede cambiar la carrera del aula porque tiene alumnos matriculados o profesores inscritos" });
                }

                var cursoActual = await _context.GetCursoActualAsync();

                foreach (var profesorAula in request.ProfesorAulas)
                {
                    if (!profesores.ContainsKey(profesorAula.ProfesorId))
                    {
                        return Conflict(new { message = $"El profesor con ID {profesorAula.ProfesorId} no existe o está inactivo." });
                    }

                    profesorAula.Asignatura = profesores[profesorAula.ProfesorId];
                    profesorAula.CursoId = cursoActual.Id;

                    bool existe = aula.AulaProfesores.Any(ap =>
                        ap.Asignatura == profesorAula.Asignatura &&
                        ap.CursoId == cursoActual.Id &&
                        ap.ProfesorId != profesorAula.ProfesorId);

                    if (existe)
                    {
                        return Conflict(new { message = $"Ya existe un profesor asignado a la asignatura {profesorAula.Asignatura} en esta aula para el año escolar actual." });
                    }
                }

                var asignaturasValidas = aula.Carrera == Carrera.Ciencias
                    ? new[] { Asignatura.Matematicas, Asignatura.Informatica, Asignatura.EducacionFisica }
                    : new[] { Asignatura.Español, Asignatura.Historia, Asignatura.EducacionFisica };

                foreach (var ap in request.ProfesorAulas)
                {
                    if (!asignaturasValidas.Contains(ap.Asignatura))
                    {
                        return Conflict(new { message = $"No se puede asignar la asignatura {ap.Asignatura} a un aula de la carrera {aula.Carrera}." });
                    }
                }

                foreach (var nuevoAP in request.ProfesorAulas)
                {
                    bool yaAsignado = aula.AulaProfesores.Any(ap =>
                        ap.Asignatura == nuevoAP.Asignatura &&
                        ap.CursoId == cursoActual.Id);

                    if (yaAsignado && !aula.AulaProfesores.Any(ap =>
                        ap.ProfesorId == nuevoAP.ProfesorId &&
                        ap.Asignatura == nuevoAP.Asignatura &&
                        ap.CursoId == cursoActual.Id))
                    {
                        return Conflict(new
                        {
                            message = $"No se puede cambiar el profesor de la asignatura {nuevoAP.Asignatura} en el curso escolar actual porque ya hay un profesor asignado."
                        });
                    }
                }

                aula.Carrera = request.Carrera;

                if (request.ProfesorAulas != null && request.ProfesorAulas.Any())
                {
                    var profesoresParaEliminar = aula.AulaProfesores
                        .Where(apExistente => !request.ProfesorAulas
                            .Any(apNuevo => apNuevo.ProfesorId == apExistente.ProfesorId))
                        .ToList();

                    foreach (var profesorEliminar in profesoresParaEliminar)
                    {
                        _context.AulaProfesores.Remove(profesorEliminar);
                    }

                    foreach (var apRequest in request.ProfesorAulas)
                    {
                        var profesorExistente = aula.AulaProfesores
                            .FirstOrDefault(ap => ap.ProfesorId == apRequest.ProfesorId);

                        if (profesorExistente == null)
                        {
                            var nuevoAulaProfesor = new AulaProfesor
                            {
                                AulaId = aula.Id,
                                ProfesorId = apRequest.ProfesorId,
                                Asignatura = apRequest.Asignatura,
                                CursoId = apRequest.CursoId
                            };
                            _context.AulaProfesores.Add(nuevoAulaProfesor);
                        }
                        else
                        {
                            profesorExistente.Asignatura = apRequest.Asignatura;
                            profesorExistente.CursoId = apRequest.CursoId;
                            _context.AulaProfesores.Update(profesorExistente);
                        }
                    }
                }
                else
                {
                    var todosProfesores = aula.AulaProfesores.ToList();
                    foreach (var profesor in todosProfesores)
                    {
                        _context.AulaProfesores.Remove(profesor);
                    }
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    _context.Aulas.Update(aula);
                    await _context.SaveChangesAsync();

                    await _trazaService.RegistrarTrazaAsync(
                        $"Aula editada: {aula.Id}, Carrera: {aula.Carrera}",
                        "UpdateAula",
                        User.Identity?.Name ?? "Desconocido");

                    await transaction.CommitAsync();

                    var aulaActualizada = await _context.Aulas
                        .Include(a => a.AulaProfesores)
                            .ThenInclude(ap => ap.Profesor)
                        .Include(a => a.Matriculas.Where(m => m.Alumno.Activo == true && m.CursoId == cursoActual.Id))
                        .FirstOrDefaultAsync(a => a.Id == aula.Id);

                    if (aulaActualizada == null)
                    {
                        return NotFound($"No se encontró el aula actualizada");
                    }

                    return Ok(new AulaDetallesDto
                    {
                        Id = aulaActualizada.Id,
                        Numero = aulaActualizada.Numero,
                        Carrera = aulaActualizada.Carrera,
                        ProfesorAulas = aulaActualizada.AulaProfesores.Select(ap => new ProfesorAulaDto
                        {
                            ProfesorId = ap.ProfesorId,
                        }).ToList(),
                        Matriculas = aulaActualizada.Matriculas.Select(m => new MatriculaAulaDto
                        {
                            alumnoId = m.AlumnoId,
                        }).ToList()
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine(ex.Message);
                    return StatusCode(500, "Error interno al actualizar aula");
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