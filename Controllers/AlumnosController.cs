using EscuelaCore.Data;
using EscuelaCore.Dto.ControllersDto;
using EscuelaCore.Dto.RequestsDto;
using EscuelaCore.Dto.SharedDto;
using EscuelaCore.Enums;
using EscuelaCore.Models;
using EscuelaCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EscuelaCore.Controllers
{

    /// <summary>
    /// Controlador para la gestión de alumnos.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AlumnosController : ControllerBase
    {
        private readonly IAulaService _aulaService;
        private readonly EscuelaCoreContext _context;
        private readonly ITrazaService _trazaService;
        private readonly IUsuarioService _usuarioService;

        public AlumnosController(
            IUsuarioService usuarioService,
            ITrazaService trazaService,
            EscuelaCoreContext context,
            IAulaService aulaService)
        {
            _usuarioService = usuarioService;
            _trazaService = trazaService;
            _context = context;
            _aulaService = aulaService;
        }

        /// <summary>
        /// Obtiene la lista paginada de alumnos activos.
        /// </summary>
        /// <param name="busqueda">Texto para buscar por nombre, apellidos o CI.</param>
        /// <param name="filtroCI">Filtro exacto por CI.</param>
        /// <param name="pagina"></param>
        /// <param name="cantidadDeRegistrosPorPagina"></param>
        /// <returns>Lista paginada de alumnos.</returns>
        [HttpGet]
        [Authorize(Roles = "Administrador,Profesor")]
        public async Task<ActionResult<Paginacion<AlumnoDto>>> GetAlumnos(
            [FromQuery] string busqueda = "",
            [FromQuery] string filtroCI = "",
            [FromQuery] int pagina = 1,
            [FromQuery] int cantidadDeRegistrosPorPagina = 5)
        {
            try
            {

                var alumnosQuery = _context.Alumnos
                    .Where(a => a.Activo)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(busqueda))
                    alumnosQuery = alumnosQuery.Where(a =>
                        a.Nombre.ToLower().Contains(busqueda.ToLower()) ||
                        a.Apellidos.ToLower().Contains(busqueda.ToLower()) ||
                        a.CI.Contains(busqueda));

                if (!string.IsNullOrEmpty(filtroCI))
                    alumnosQuery = alumnosQuery.Where(a => a.CI == filtroCI);

                int totalDeRegistros = await alumnosQuery.CountAsync();

                var alumnos = alumnosQuery
                    .Skip((pagina - 1) * cantidadDeRegistrosPorPagina)
                    .Take(cantidadDeRegistrosPorPagina)
                    .Select(a => new AlumnoListaDto
                    {
                        Id = a.Id,
                        CI = a.CI,
                        NombreCompleto = $"{a.Nombre} {a.Apellidos}",
                    })
                    .ToList();

                if (totalDeRegistros == 0)
                {
                    return Ok(new { message = "No se encontraron alumnos" });
                }

                var model = new Paginacion<AlumnoListaDto>
                {
                    Datos = alumnos,
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
        /// Obtiene los detalles de un alumno por su Id.
        /// </summary>
        /// <param name="id">Id del alumno.</param>
        /// <returns>Datos de un alumno.</returns>
        [HttpGet("{id}")]
        [Authorize(Roles = "Administrador,Profesor")]
        public async Task<ActionResult<AlumnoDto>> GetAlumno(int id)
        {
            try
            {
                var alumno = await _context.Alumnos
                    .Include(p => p.Usuario)
                    .Where(a => a.Activo)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (alumno == null)
                {
                    return NotFound($"No se encontró el alumno con id {id}");
                }

                var model = new AlumnoDto
                {
                    Id = alumno.Id,
                    Nombre = $"{alumno.Nombre} {alumno.Apellidos}",
                    CI = alumno.CI,
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
        /// Crea un nuevo alumno y lo matricula en el curso actual.
        /// </summary>
        /// <param name="request">Datos del alumno a crear.</param>
        /// <returns>Datos del alumno creado.</returns>
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<AlumnoDto>> CreateAlumno([FromBody] CreateAlumnoRequestDto request)
        {
            try
            {
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

                using var transaction = await _context.Database.BeginTransactionAsync();
                string esAlumno = "alumno";

                try
                {
                    string usuarioId = await _usuarioService.CrearUsuarioAsync(
                        request.UsuarioEmail,
                        request.Contraseña,
                        esAlumno);

                    var alumno = new Alumno
                    {
                        Nombre = request.Nombre,
                        Apellidos = request.Apellidos,
                        CI = request.CI,
                        UsuarioId = usuarioId
                    };
                    _context.Alumnos.Add(alumno);
                    await _context.SaveChangesAsync();

                    await _trazaService.RegistrarTrazaAsync(
                        $"Alumno creado: {alumno.Nombre} {alumno.Apellidos} (CI: {alumno.CI})",
                        "CreateAlumno",
                        User.Identity?.Name ?? "Desconocido");

                    await transaction.CommitAsync();

                    return CreatedAtAction(nameof(GetAlumno),
                        new { id = alumno.Id },
                        new AlumnoDto
                        {
                            Id = alumno.Id,
                            Nombre = $"{alumno.Nombre} {alumno.Apellidos}",
                            CI = alumno.CI,
                        });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    await transaction.RollbackAsync();
                    return StatusCode(500, "Error interno al crear alumno");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Actualiza los datos de un alumno existente.
        /// </summary>
        /// <param name="id">Id del alumno a actualizar.</param>
        /// <param name="request">Datos actualizados del alumno.</param>
        /// <returns>Datos del alumno actualizado.</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<ActionResult<AlumnoDto>> UpdateAlumno(int id, [FromBody] UpdateAlumnoRequestDto request)
        {
            try
            {
                bool nombreDuplicadoAlumno = await _context.Alumnos.AnyAsync(a =>
                    a.Nombre == request.Nombre &&
                    a.Apellidos == request.Apellidos &&
                    a.Id != id);
                bool nombreDuplicadoProfesor = await _context.Profesores.AnyAsync(p =>
                    p.Nombre == request.Nombre &&
                    p.Apellidos == request.Apellidos);
                bool ciDuplicadoAlumno = await _context.Alumnos.AnyAsync(a => a.CI == request.CI && a.Id != id);
                bool ciDuplicadoProfesor = await _context.Profesores.AnyAsync(p => p.CI == request.CI);

                if (!ModelState.IsValid)
                    return BadRequest(new
                    {
                        Message = "Datos de entrada inválidos",
                        Errors = ModelState.Values.SelectMany(err => err.Errors)
                    });

                var alumno = await _context.Alumnos
                    .Where(a => a.Activo)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (alumno == null)
                {
                    return NotFound(new { Message = "Alumno no encontrado" });
                }

                if (ciDuplicadoAlumno || ciDuplicadoProfesor)
                {
                    return Conflict(new { Message = "Ya existe una persona con ese carnet de identidad." });
                }

                if (nombreDuplicadoAlumno || nombreDuplicadoProfesor)
                {
                    return Conflict(new { Message = "Ya existe una persona con ese nombre y apellidos." });
                }

                alumno.Nombre = request.Nombre;
                alumno.Apellidos = request.Apellidos;
                alumno.CI = request.CI;

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    _context.Alumnos.Update(alumno);
                    await _context.SaveChangesAsync();

                    await _trazaService.RegistrarTrazaAsync(
                        $"Alumno editado: {alumno.Nombre} {alumno.Apellidos} (CI: {alumno.CI})",
                        "UpdateAlumno",
                        User.Identity?.Name ?? "Desconocido");

                    await transaction.CommitAsync();

                    return Ok(new AlumnoDto
                    {
                        Id = alumno.Id,
                        Nombre = $"{alumno.Nombre} {alumno.Apellidos}",
                        CI = alumno.CI,
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine(ex.Message);
                    return StatusCode(500, "Error interno al actualizar alumno");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Da de baja a un alumno (desactiva su cuenta).
        /// </summary>
        /// <param name="id">Id del alumno a dar de baja.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DarBajaAlumno(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var alumno = await _context.Alumnos
                    .Where(a => a.Activo)
                    .Include(a => a.Usuario)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (alumno == null)
                    return NotFound(new { message = "Alumno no encontrado" });

                alumno.Activo = false;
                alumno.Aula = null;
                if (alumno.Usuario != null)
                {
                    alumno.Usuario.Activo = false;
                }
                else
                {
                    Console.WriteLine($"Advertencia: Profesor no tiene usuario asociado");
                }
                alumno.FechaBaja = DateTime.Now;

                _context.Alumnos.Update(alumno);
                await _context.SaveChangesAsync();

                await _trazaService.RegistrarTrazaAsync(
                    $"Alumno dado de baja: {alumno.Nombre} {alumno.Apellidos} (CI: {alumno.CI})",
                    "DarBajaAlumno",
                    User.Identity?.Name ?? "Desconocido");

                await transaction.CommitAsync();

                return Ok(new { message = "Alumno dado de baja correctamente" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno al dar de baja alumno");
            }
        }

        /// <summary>
        /// Restaura (reactiva) un alumno dado de baja previamente.
        /// </summary>
        /// <param name="id">Id del alumno a restaurar.</param>
        /// <returns>Resultado de la operación.</returns>
        [HttpPost("{id}/restaurar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> RestaurarAlumno(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var alumno = await _context.Alumnos
                    .Where(a => a.Activo == false)
                    .Include(a => a.Usuario)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (alumno == null)
                    return NotFound(new { message = "Alumno no encontrado" });

                if (alumno.Activo)
                    return Conflict(new { message = "El alumno ya está activo" });

                alumno.Activo = true;
                if (alumno.Usuario != null)
                {
                    alumno.Usuario.Activo = true;
                }
                else
                {
                    Console.WriteLine($"Advertencia: Profesor no tiene usuario asociado");
                }
                alumno.FechaBaja = default;
                alumno.FechaRestauracion = DateTime.Now;

                _context.Alumnos.Update(alumno);
                await _context.SaveChangesAsync();

                await _trazaService.RegistrarTrazaAsync(
                    $"Alumno restaurado: {alumno.Nombre} {alumno.Apellidos} (CI: {alumno.CI})",
                    "RestaurarAlumno",
                    User.Identity?.Name ?? "Desconocido");

                await transaction.CommitAsync();

                return Ok(new { message = "Alumno restaurado correctamente" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno al restaurar alumno");
            }
        }

        /// <summary>
        /// Obtiene las evaluaciones de un alumno por su Id.
        /// </summary>
        /// <param name="id">Id del alumno.</param>
        /// <param name="pagina"></param>
        /// <param name="cantidadDeRegistrosPorPagina"></param>
        /// <returns>Lista paginada de evaluaciones del alumno.</returns>
        [HttpGet("{id}/evaluaciones")]
        [Authorize(Roles = "Administrador,Alumno")]
        public async Task<ActionResult<List<EvaluacionDto>>> GetEvaluacionesAlumno(int id,
            [FromQuery] int pagina = 1,
            [FromQuery] int cantidadDeRegistrosPorPagina = 5)
        {
            try
            {
                var evaluacionesQuery = _context.Evaluaciones
                    .Where(e => e.Alumno.Id == id)
                    .AsQueryable();
                int totalDeRegistros = await evaluacionesQuery.CountAsync();

                var evaluacionesAlumno = evaluacionesQuery
                    .Skip((pagina - 1) * cantidadDeRegistrosPorPagina)
                    .Take(cantidadDeRegistrosPorPagina)
                    .Select(e => new EvaluacionDto
                    {
                        Asignatura = e.Asignatura,
                        Nota = e.Nota,
                        FechaEvaluacion = e.FechaEvaluacion
                    })
                    .ToList();

                if (totalDeRegistros == 0)
                {
                    return Ok($"No se encontraron evaluaciones");
                }

                var model = new Paginacion<EvaluacionDto>
                {
                    Datos = evaluacionesAlumno,
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