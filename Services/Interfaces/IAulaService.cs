using EscuelaCore.Enums;

namespace EscuelaCore.Services.Interfaces
{
    public interface IAulaService
    {
        Task<bool> AulaTieneClaustroCompleto(int aulaId, Carrera carrera);
    }
}
