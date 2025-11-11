using EscuelaCore.Data;
using EscuelaCore.Dto.RequestsDto;
using EscuelaCore.Models;
using EscuelaCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EscuelaCore.Controllers
{
    /// <summary>
    /// Controlador para autenticación y registro de usuarios.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly EscuelaCoreContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly IJwtService _jwtService;
        private readonly ITrazaService _trazaService;
        private readonly IUsuarioService _usuarioService;

        public AuthController(
            EscuelaCoreContext context,
            UserManager<Usuario> userManager,
            IJwtService jwtService,
            ITrazaService trazaService,
            IUsuarioService usuarioService)
        {
            _context = context;
            _userManager = userManager;
            _jwtService = jwtService;
            _trazaService = trazaService;
            _usuarioService = usuarioService;
        }

        /// <summary>
        /// Crea un nuevo usuario Administrador.
        /// </summary>
        /// <param name="request">Datos del usuario a crear.</param>
        /// <returns>Id del usuario creado.</returns>
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AdminUserCreate([FromBody] CreateAdminRequestDto request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                string esAdministrador = "administrador";
                bool usuarioDuplicado = await _context.Usuarios.AnyAsync(u => u.UserName == request.UsuarioEmail);

                if (!ModelState.IsValid)
                    return BadRequest(new
                    {
                        Message = "Datos de entrada inválidos",
                        Errors = ModelState.Values.SelectMany(err => err.Errors)
                    });

                if (usuarioDuplicado)
                {
                    return Conflict(new { Message = "Ya existe un usuario con ese email." });
                }

                var usuarioId = await _usuarioService.CrearUsuarioAsync(
                        request.UsuarioEmail,
                        request.Contraseña,
                        esAdministrador);

                await transaction.CommitAsync();

                return Ok(new { Message = $"Usuario de administrador {request.UsuarioEmail}, creado." });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Inicia sesión y genera un token JWT para el usuario.
        /// </summary>
        /// <param name="request">Datos de acceso del usuario.</param>
        /// <returns>Token JWT y datos del usuario autenticado.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { message = "Datos de entrada inválidos" });

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
                    return Unauthorized(new { message = "Credenciales inválidas" });

                if (!user.Activo)
                    return Unauthorized(new { message = "Usuario desactivado" });

                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName ?? "Desconocido"),
                    new Claim(ClaimTypes.Email, user.Email ?? "Desconocido"),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("userId", user.Id)
                };

                foreach (var role in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, role));
                }

                var token = _jwtService.GenerateJwtToken(authClaims);

                await _trazaService.RegistrarTrazaAsync(
                    $"Login exitoso: {user.Email}",
                    "Auth",
                    user.UserName ?? "Desconocido");

                return Ok(new
                {
                    token,
                    expiration = DateTime.Now.AddHours(3),
                    roles = userRoles,
                    email = user.Email,
                    nombre = user.UserName,
                });
            }
            catch (Exception ex)
            {
                await _trazaService.RegistrarTrazaAsync(
                    $"Error en login: {ex.Message}",
                    "Auth");
                
                Console.WriteLine($"=== ERROR EN LOGIN ===");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }
    }
}