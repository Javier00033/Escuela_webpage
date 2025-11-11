using EscuelaCore.Enums;

namespace EscuelaCore.Services.Interfaces
{
    public interface IAlumnoService
    {
        Task<bool> AlumnoAproboCarrera(int alumnoId, Carrera carrera);
        List<Asignatura> GetAsignaturasPorCarrera(Carrera carrera);
        Task<Carrera?> ObtenerCarreraAprobada(int alumnoId);
        Task<bool> PuedeMatricularseEnCarrera(int alumnoId, Carrera carrera);
        Task<bool> AlumnoTieneCarreraAprobada(int alumnoId);
    }
}
