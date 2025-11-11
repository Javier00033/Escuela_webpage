using EscuelaCore.Models;
using EscuelaCore.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

public class UsuarioService : IUsuarioService
{
    private readonly UserManager<Usuario> _userManager;
    private readonly ITrazaService _trazaService;

    public UsuarioService(UserManager<Usuario> userManager, ITrazaService trazaService)
    {
        _userManager = userManager;
        _trazaService = trazaService;
    }

    /// <summary>
    /// Crea un usuario automáticamente.
    /// </summary>
    public async Task<string> CrearUsuarioAsync(string usuarioEmail, string contraseña, string rol)
    {
        try
        {
            var usuarioExistente = await _userManager.FindByEmailAsync(usuarioEmail);

            if (usuarioExistente != null)
            {
                throw new Exception($"Ya existe un usuario con el email {usuarioEmail}");
            }

            var usuario = new Usuario
            {
                UserName = usuarioEmail,
                Email = usuarioEmail,
                Activo = true,
                EmailConfirmed = true
            };

            var resultado = await _userManager.CreateAsync(usuario, contraseña);
            if (!resultado.Succeeded)
            {
                var errores = string.Join(", ", resultado.Errors.Select(e => e.Description));
                throw new Exception($"Error al crear usuario: {errores}");
            }

            if (rol == "alumno")
            {
                var rolResultado = await _userManager.AddToRoleAsync(usuario, "Alumno");
                if (!rolResultado.Succeeded)
                {
                    await _userManager.DeleteAsync(usuario);
                    throw new Exception("Error al asignar rol de Alumno");
                }

                await _trazaService.RegistrarTrazaAsync(
                    $"Usuario creado para alumno:({usuarioEmail})",
                    "CrearUsuarioAsync",
                    "Sistema");
            }
            else if (rol == "profesor")
            {
                var rolResultado = await _userManager.AddToRoleAsync(usuario, "Profesor");
                if (!rolResultado.Succeeded)
                {
                    await _userManager.DeleteAsync(usuario);
                    throw new Exception("Error al asignar rol de Profesor");
                }

                await _trazaService.RegistrarTrazaAsync(
                    $"Usuario creado para profesor:({usuarioEmail})",
                    "CrearUsuarioAsync",
                    "Sistema");
            }
            else if (rol == "administrador")
            {
                var rolResultado = await _userManager.AddToRoleAsync(usuario, "Administrador");
                if (!rolResultado.Succeeded)
                {
                    await _userManager.DeleteAsync(usuario);
                    throw new Exception("Error al asignar rol de Administrador");
                }

                await _trazaService.RegistrarTrazaAsync(
                    $"Usuario creado para administrador:({usuarioEmail})",
                    "CrearUsuarioAsync",
                    "Sistema");
            }
            return usuario.Id;
        }
        catch (Exception ex)
        {
            await _trazaService.RegistrarTrazaAsync(
                $"Error creando usuario: {ex.Message}",
                "CrearUsuarioAsync",
                "Sistema");
            throw;
        }
    }
}
