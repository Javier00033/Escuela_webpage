namespace EscuelaCore.Services.Interfaces
{
    public interface ITrazaService
    {
        Task RegistrarTrazaAsync(string mensaje, string operacion, string usuario = null);
    }
}
