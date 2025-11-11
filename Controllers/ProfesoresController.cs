using Azure.Core;
using EscuelaCore.Data;
using EscuelaCore.Dto.ControllersDto;
using EscuelaCore.Dto.RequestsDto;
using EscuelaCore.Dto.SharedDto;
using EscuelaCore.Enums;
using EscuelaCore.Models;
using EscuelaCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EscuelaCore.Controllers
{

    /// <summary>
    /// Controlador para la gestión de profesores.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfesoresController : ControllerBase
    {
        private readonly EscuelaCoreContext _context;
        private readonly ITrazaService _trazaService;
        private readonly IUsuarioService _usuarioService;

        public ProfesoresController(
            IUsuarioService usuarioService,
            ITrazaService trazaService,
            EscuelaCoreContext context)
        {
            _usuarioService = usuarioService;
            _trazaService = trazaService;
            _context = context;
        }

        /// <summary>
        /// Obtiene la lista paginada de profesores activos.
        /// </summary>
        /// <param name="busqueda">Texto para buscar por nombre, apellidos o CI.</param>
        /// <param name="filtroCI">Filtro exacto por CI.</param>
        /// <param name="pagina"></param>
        /// <param name="cantidadDeRegistrosPorPagina"></param>
        /// <returns>Lista paginada de profesores.</returns>
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<Paginacion<ProfesorDto>>> GetProfesores(
            [FromQuery] string? busqueda = null,
            [FromQuery] string? filtroCI = null,
            [FromQuery] int pagina = 1,
            [FromQuery] int cantidadDeRegistrosPorPagina = 5)
        {
            try
            {
                var profesoresQuery = _context.Profesores
                    .Include(a => a.AulaProfesores)
                    .Where(a => a.Activo)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(busqueda))
                    profesoresQuery = profesoresQuery.Where(p =>
                        p.Nombre.ToLower().Contains(busqueda.ToLower()) ||
                        p.Apellidos.ToLower().Contains(busqueda.ToLower()) ||
                        p.CI.Contains(busqueda));

                if (!string.IsNullOrEmpty(filtroCI))
                    profesoresQuery = profesoresQuery.Where(p => p.CI == filtroCI);

                int totalDeRegistros = await profesoresQuery.CountAsync();

                var profesores = profesoresQuery
                    .Skip((pagina - 1) * cantidadDeRegistrosPorPagina)
                    .Take(cantidadDeRegistrosPorPagina)
                    .Select(p => new ProfesorDto
                    {
                        Id = p.Id,
                        Nombre = $"{p.Nombre} {p.Apellidos}",
                        CI = p.CI,
                        Asignatura = p.Asignatura,
                        AulaProfesores = p.AulaProfesores.Select(ap => new AulaProfesorDto
                        {
                            AulaId = ap.AulaId
                        }).ToList()
                    })
                    .ToList();

                if (totalDeRegistros == 0)
                {
                    return Ok("No se encontraron profesores");
                }

                var model = new Paginacion<ProfesorDto>
                {
                    Datos = profesores,
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
        /// Obtiene los detalles de un profesor por su id.
        /// </summary>
        /// <param name="id">Id del profesor.</param>
        /// <returns>Datos de un profesor.</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<ProfesorDto>> GetProfesor(int id)
        {
            try
            {
                var profesor = await _context.Profesores
                    .Include(p => p.AulaProfesores)
                    .Where(p => p.Activo)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (profesor == null)
                {
                    return NotFound($"No se encontró el profesor con id {id}");
                }

                var model = new ProfesorDto
                {
                    Id = profesor.Id,
                    Nombre = $"{profesor.Nombre} {profesor.Apellidos}",
                    CI = profesor.CI,
                    Asignatura = profesor.Asignatura,
                    AulaProfesores = profesor.AulaProfesores.Select(ap => new AulaProfesorDto
                    {
                        AulaId = ap.AulaId
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
        /// Crea un nuevo profesor y le asigna aulas.
        /// </summary>
        /// <param name="request">Datos del profesor a crear.</param>
        /// <returns>Datos del profesor creado.</returns>
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<ProfesorDto>> CreateProfesor([FromBody] CreateProfesorRequestDto request)
        {
            try
            {
                var cursoActual = await _context.GetCursoActualAsync();
                bool nombreDuplicadoAlumno = await _context.Alumnos.AnyAsync(a =>
                    a.Nombre == request.Nombre &&
                    a.Apellidos == request.Apellidos);
                bool nombreDuplicadoProfesor = await _context.Profesores.AnyAsync(p =>
                    p.Nombre == request.Nombre &&
                    p.Apellidos == request.Apellidos);
                bool ciDuplicadoAlumno = await _context.Alumnos.AnyAsync(a => a.CI == request.CI);
                bool ciDuplicadoProfesor = await _context.Profesores.AnyAsync(p => p.CI == request.CI);
                bool usuarioDuplicado = await _context.Usuarios.AnyAsync(u => u.UserName == request.UsuarioEmail);

                if (!ModelState.IsValid)
                    return BadRequest(new
                    {
                        Message = "Datos de entrada inválidos",
                        Errors = ModelState.Values.SelectMany(err => err.Errors)
                    });

                if (ciDuplicadoAlumno || ciDuplicadoProfesor)
                {
                    return Conflict(new { Message = "Ya existe una persona con ese carnet de identidad." });
                }

                if (nombreDuplicadoAlumno || nombreDuplicadoProfesor)
                {
                    return Conflict(new { Message = "Ya existe una persona con ese nombre y apellidos." });
                }

                if (usuarioDuplicado)
                {
                    return Conflict(new { Message = "Ya existe un usuario con ese email." });
                }

                var aulaIds = request.AulaProfesores.Select(ap => ap.AulaId).Distinct().ToList();
                var aulas = await _context.Aulas
                    .Include(a => a.AulaProfesores)
                    .Where(a => aulaIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var nuevoAP in request.AulaProfesores)
                {
                    var aula = aulas.FirstOrDefault(a => a.Id == nuevoAP.AulaId);
                    if (aula == null)
                        return Conflict(new { message = $"El aula con Id {nuevoAP.AulaId} no existe." });

                    var asignaturasValidas = aula.Carrera == Carrera.Ciencias
                        ? new[] { Asignatura.Matematicas, Asignatura.Informatica, Asignatura.EducacionFisica }
                        : new[] { Asignatura.Español, Asignatura.Historia, Asignatura.EducacionFisica };

                    if (!asignaturasValidas.Contains(request.Asignatura))
                    {
                        return Conflict(new { message = $"No se puede asignar la asignatura {request.Asignatura} a un aula de la carrera {aula.Carrera}." });
                    }
                }

                foreach (var nuevoAP in request.AulaProfesores)
                {
                    var AulaConProfesorYaAsignado = await _context.AulaProfesores.AnyAsync(ap =>
                        ap.AulaId == nuevoAP.AulaId &&
                        ap.Asignatura == request.Asignatura &&
                        ap.CursoId == cursoActual.Id);

                    if (AulaConProfesorYaAsignado)
                    {
                        return Conflict(new { message = $"Ya existe un profesor asignado a la asignatura en esta aula para el año escolar actual." });
                    }
                }
                using var transaction = await _context.Database.BeginTransactionAsync();
                string esProfesor = "profesor";

                try
                {
                    var aulaProfesoresEntities = request.AulaProfesores.Select(ap => new AulaProfesor
                    {
                        AulaId = ap.AulaId,
                        Asignatura = request.Asignatura,
                        CursoId = cursoActual.Id,
                        ProfesorId = 0
                    }).ToList();

                    string usuarioId = await _usuarioService.CrearUsuarioAsync(
                        request.UsuarioEmail,
                        request.Contraseña,
                        esProfesor);

                    var profesor = new Profesor
                    {
                        Nombre = request.Nombre,
                        Apellidos = request.Apellidos,
                        CI = request.CI,
                        UsuarioId = usuarioId,
                        Asignatura = request.Asignatura,
                        AulaProfesores = aulaProfesoresEntities,
                    };

                    _context.Profesores.Add(profesor);
                    await _context.SaveChangesAsync();

                    await _trazaService.RegistrarTrazaAsync(
                        $"Profesor creado: {profesor.Nombre} {profesor.Apellidos} (CI: {profesor.CI})",
                        "CreateProfesor",
                        User.Identity?.Name ?? "Desconocido");

                    await transaction.CommitAsync();

                    return CreatedAtAction(nameof(GetProfesor),
                        new { id = profesor.Id },
                        new ProfesorDto
                        {
                            Id = profesor.Id,
                            Nombre = $"{profesor.Nombre} {profesor.Apellidos}",
                            CI = profesor.CI,
                            Asignatura = profesor.Asignatura,
                            AulaProfesores = profesor.AulaProfesores.Select(ap => new AulaProfesorDto
                            {
                                AulaId = ap.AulaId
                            }).ToList()
                        });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine(ex.Message);
                    return StatusCode(500, "Error interno al crear profesor");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Actualiza los datos de un profesor existente.
        /// </summary>
        /// <param name="id">Id del profesor a actualizar.</param>
        /// <param name="request">Datos actualizados del profesor.</param>
        /// <returns>Datos del profesor actualizado.</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<ProfesorDto>> UpdateProfesor(int id, [FromBody] UpdateProfesorRequestDto request)
        {
            try
            {
                var cursoActual = await _context.GetCursoActualAsync();

                bool nombreDuplicadoAlumno = await _context.Alumnos.AnyAsync(a =>
                    a.Nombre == request.Nombre &&
                    a.Apellidos == request.Apellidos &&
                    a.Id != id);
                bool nombreDuplicadoProfesor = await _context.Profesores.AnyAsync(p =>
                    p.Nombre == request.Nombre &&
                    p.Apellidos == request.Apellidos &&
                    p.Id != id);
                bool ciDuplicadoAlumno = await _context.Alumnos.AnyAsync(a => a.CI == request.CI);
                bool ciDuplicadoProfesor = await _context.Profesores.AnyAsync(p => p.CI == request.CI && p.Id != id);

                if (!ModelState.IsValid)
                    return BadRequest(new
                    {
                        Message = "Datos de entrada inválidos",
                        Errors = ModelState.Values.SelectMany(err => err.Errors)
                    });

                var profesor = await _context.Profesores
                    .Include(p => p.AulaProfesores)
                    .Where(p => p.Activo)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (profesor == null)
                {
                    return NotFound(new { message = "Profesor no encontrado" });
                }

                bool estaCambiandoAsignatura = profesor.Asignatura != request.Asignatura;
                bool tieneAulasExistentes = profesor.AulaProfesores.Any();
                bool seEstanAsignandoAulas = request.AulaProfesores != null && request.AulaProfesores.Any();
                bool hayAulasInvolucradas = tieneAulasExistentes || seEstanAsignandoAulas;

                if (estaCambiandoAsignatura && hayAulasInvolucradas)
                {
                    return Conflict(new { message = "No se le puede cambiar la asignatura al profesor porque tiene aulas asignadas o se le están asignando aulas." });
                }

                if (!profesor.Activo)
                    return Conflict(new { message = "El profesor está dado de baja y no puede ser asignado a aulas." });

                if (ciDuplicadoAlumno || ciDuplicadoProfesor)
                {
                    return Conflict(new { Message = "Ya existe una persona con ese carnet de identidad." });
                }

                if (nombreDuplicadoAlumno || nombreDuplicadoProfesor)
                {
                    return Conflict(new { Message = "Ya existe una persona con ese nombre y apellidos." });
                }

                var aulaIds = request.AulaProfesores.Select(ap => ap.AulaId).Distinct().ToList();
                var aulas = await _context.Aulas
                    .Where(a => aulaIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var ap in request.AulaProfesores)
                {
                    var aula = aulas.FirstOrDefault(a => a.Id == ap.AulaId);
                    if (aula == null)
                        return Conflict(new { message = $"El aula con Id {ap.AulaId} no existe." });

                    var asignaturasValidas = aula.Carrera == Carrera.Ciencias
                        ? new[] { Asignatura.Matematicas, Asignatura.Informatica, Asignatura.EducacionFisica }
                        : new[] { Asignatura.Español, Asignatura.Historia, Asignatura.EducacionFisica };

                    if (!asignaturasValidas.Contains(request.Asignatura))
                    {
                        return Conflict(new { message = $"No se puede asignar la asignatura {request.Asignatura} a un aula de la carrera {aula.Carrera}." });
                    }
                }

                foreach (var nuevoAP in request.AulaProfesores)
                {
                    bool existe = await _context.AulaProfesores.AnyAsync(ap =>
                        ap.AulaId == nuevoAP.AulaId &&
                        ap.Asignatura == request.Asignatura &&
                        ap.CursoId == cursoActual.Id &&
                        ap.Profesor.Id != id);

                    if (existe)
                    {
                        return Conflict(new { message = $"Ya existe un profesor asignado a la asignatura en el aula para el año escolar actual." });
                    }
                }

                foreach (var nuevoAP in request.AulaProfesores)
                {
                    bool yaAsignado = await _context.AulaProfesores.AnyAsync(ap =>
                        ap.AulaId == nuevoAP.AulaId &&
                        ap.Asignatura == request.Asignatura &&
                        ap.CursoId == cursoActual.Id);

                    if (yaAsignado && !await _context.AulaProfesores.AnyAsync(ap =>
                        ap.AulaId == nuevoAP.AulaId &&
                        ap.Asignatura == request.Asignatura &&
                        ap.CursoId == cursoActual.Id &&
                        ap.Profesor.Id == id))
                    {
                        return Conflict(new
                        {
                            message = $"No se puede cambiar el profesor de la asignatura en el aula para el año escolar actual porque ya hay un profesor asignado."
                        });
                    }
                }

                var tieneAlumnosMatriculados = await _context.AulaProfesores
                    .Where(ap => ap.Profesor.Id == id)
                    .AnyAsync(ap => ap.Aula.Alumnos.Any(a => a.Activo));

                if (profesor.Asignatura != request.Asignatura && tieneAlumnosMatriculados)
                {
                    return Conflict(new { message = "No se puede cambiar la asignatura del profesor porque tiene alumnos matriculados en sus aulas." });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    profesor.Nombre = request.Nombre;
                    profesor.Apellidos = request.Apellidos;
                    profesor.CI = request.CI;
                    profesor.Asignatura = request.Asignatura;

                    if (request.AulaProfesores != null)
                    {
                        var aulasParaEliminar = profesor.AulaProfesores
                            .Where(apExistente => !request.AulaProfesores
                                .Any(apNuevo => apNuevo.AulaId == apExistente.AulaId))
                            .ToList();

                        foreach (var aulaEliminar in aulasParaEliminar)
                        {
                            _context.AulaProfesores.Remove(aulaEliminar);
                        }

                        foreach (var apRequest in request.AulaProfesores)
                        {
                            var aulaExistente = profesor.AulaProfesores
                                .FirstOrDefault(ap => ap.AulaId == apRequest.AulaId);

                            if (aulaExistente == null)
                            {
                                var nuevaAulaProfesor = new AulaProfesor
                                {
                                    ProfesorId = profesor.Id,
                                    AulaId = apRequest.AulaId,
                                    Asignatura = request.Asignatura,
                                    CursoId = cursoActual.Id
                                };
                                _context.AulaProfesores.Add(nuevaAulaProfesor);
                            }
                            else
                            {
                                aulaExistente.CursoId = cursoActual.Id;
                                _context.AulaProfesores.Update(aulaExistente);
                            }
                        }
                    }

                    _context.Profesores.Update(profesor);
                    await _context.SaveChangesAsync();

                    await _trazaService.RegistrarTrazaAsync(
                        $"Profesor editado: {profesor.Nombre} {profesor.Apellidos} (CI: {profesor.CI})",
                        "UpdateProfesor",
                        User.Identity?.Name ?? "Desconocido");

                    await transaction.CommitAsync();

                    return Ok(new ProfesorDto
                    {
                        Nombre = $"{profesor.Nombre} {profesor.Apellidos}",
                        CI = profesor.CI,
                        Asignatura = profesor.Asignatura,
                        AulaProfesores = profesor.AulaProfesores.Select(ap => new AulaProfesorDto
                        {
                            AulaId = ap.AulaId
                        }).ToList()
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine(ex.Message);
                    return StatusCode(500, "Error interno al actualizar profesor");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Da de baja a un profesor (desactiva su cuenta).
        /// </summary>
        /// <param name="id">Id del profesor a dar de baja.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DarBajaProfesor(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var profesor = await _context.Profesores
                    .Where(p => p.Activo)
                    .Include(p => p.Usuario)
                    .Include(p => p.AulaProfesores)
                        .ThenInclude(ap => ap.Aula)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (profesor == null)
                    return NotFound(new { message = "Profesor no encontrado" });

                var tieneAulasConAlumnosActivos = await _context.AulaProfesores
                    .Where(ap => ap.Profesor.Id == id)
                    .AnyAsync(ap => ap.Aula.Alumnos.Any(a => a.Activo));

                if (profesor.AulaProfesores.Any() || tieneAulasConAlumnosActivos)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { message = "El profesor tiene aulas asignadas o las aulas que tiene tienen alumnos activos" });
                }

                profesor.AulaProfesores = new List<AulaProfesor>();
                profesor.Activo = false;
                if (profesor.Usuario != null)
                {
                    profesor.Usuario.Activo = false;
                }
                else
                {
                    Console.WriteLine($"Advertencia: Profesor no tiene usuario asociado");
                }
                profesor.FechaBaja = DateTime.Now;

                _context.Profesores.Update(profesor);
                await _context.SaveChangesAsync();

                await _trazaService.RegistrarTrazaAsync(
                    $"Profesor dado de baja: {profesor.Nombre} {profesor.Apellidos} (CI: {profesor.CI})",
                    "DarBajaProfesor",
                    User.Identity?.Name ?? "Desconocido");

                await transaction.CommitAsync();

                return Ok(new { message = "Profesor dado de baja correctamente" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno al dar de baja profesor");
            }
        }

        /// <summary>
        /// Restaura (reactiva) un profesor dado de baja previamente.
        /// </summary>
        /// <param name="id">Id del profesor a restaurar.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpPost("{id}/restaurar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> RestaurarProfesor(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var profesor = await _context.Profesores
                    .Where(p => p.Activo == false)
                    .Include(p => p.Usuario)
                    .Include(p => p.AulaProfesores)
                                .ThenInclude(ap => ap.Aula)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (profesor == null)
                    return NotFound(new { message = "Profesor no encontrado" });

                if (profesor.Activo)
                    return Conflict(new { message = "El profesor ya está activo" });

                profesor.Activo = true;
                if (profesor.Usuario != null)
                {
                    profesor.Usuario.Activo = true;
                }
                else
                {
                    Console.WriteLine($"Advertencia: Profesor no tiene usuario asociado");
                }
                profesor.FechaBaja = default;
                profesor.FechaRestauracion = DateTime.Now;

                _context.Profesores.Update(profesor);
                await _context.SaveChangesAsync();

                await _trazaService.RegistrarTrazaAsync(
                    $"Profesor restaurado: {profesor.Nombre} {profesor.Apellidos} (CI: {profesor.CI})",
                    "RestaurarProfesor",
                    User.Identity?.Name ?? "Desconocido");

                await transaction.CommitAsync();

                return Ok(new { message = "Profesor restaurado correctamente" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno al restaurar profesor");
            }
        }

        /// <summary>
        /// Obtiene las evaluaciones de un profesor por su Id.
        /// </summary>
        /// <param name="id">Id del profesor.</param>
        /// <param name="pagina"></param>
        /// <param name="cantidadDeRegistrosPorPagina"></param>
        /// <returns>Lista paginada de evaluaciones del profesor.</returns>
        [HttpGet("{id}/evaluaciones")]
        [Authorize(Roles = "Administrador,Profesor")]
        public async Task<ActionResult<List<EvaluacionDto>>> GetEvaluacionesProfesor(int id,
            [FromQuery] int pagina = 1,
            [FromQuery] int cantidadDeRegistrosPorPagina = 5)
        {
            try
            {
                var evaluacionesQuery = _context.Evaluaciones
                    .Where(e => e.Profesor.Id == id)
                    .AsQueryable();
                int totalDeRegistros = await evaluacionesQuery.CountAsync();

                var evaluacionesDelProfesor = evaluacionesQuery
                    .Include(e => e.Alumno)
                    .Skip((pagina - 1) * cantidadDeRegistrosPorPagina)
                    .Take(cantidadDeRegistrosPorPagina)
                    .Select(e => new EvaluacionDto
                    {
                        Alumno = $"{e.Alumno.Nombre} {e.Alumno.Apellidos}",
                        Asignatura = e.Asignatura,
                        Nota = e.Nota,
                        FechaEvaluacion = e.FechaEvaluacion
                    })
                    .ToList();

                if (totalDeRegistros == 0)
                {
                    return Ok($"No se encontraron evaluaciones para el profesor con ID {id}");
                }

                var model = new Paginacion<EvaluacionDto>
                {
                    Datos = evaluacionesDelProfesor,
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