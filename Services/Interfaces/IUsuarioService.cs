namespace EscuelaCore.Services.Interfaces
{
    public interface IUsuarioService
    {
        Task<string> CrearUsuarioAsync(string usuarioEmail, string contraseña, string rol);
    }
}
