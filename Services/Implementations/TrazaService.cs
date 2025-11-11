using EscuelaCore.Data;
using EscuelaCore.Models;
using EscuelaCore.Services.Interfaces;

namespace EscuelaCore.Services.Implementations
{
    /// <summary>
    /// Servicio para registrar trazas (logs de auditoría) en la base de datos.
    /// </summary>
    public class TrazaService : ITrazaService
    {
        private readonly EscuelaCoreContext _context;

        public TrazaService(EscuelaCoreContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Registra una traza en la base de datos.
        /// </summary>
        /// <param name="mensaje">Mensaje de la traza.</param>
        /// <param name="operacion">Operación realizada.</param>
        /// <param name="usuario">Usuario que realizó la operación.</param>
        public async Task RegistrarTrazaAsync(string mensaje, string operacion, string usuario = null)
        {
            try
            {
                var traza = new Traza
                {
                    Mensaje = mensaje,
                    Fecha = DateTime.Now,
                    Usuario = usuario ?? "Sistema",
                    Operacion = operacion
                };

                _context.Trazas.Add(traza);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registrando traza: {ex.Message}");
            }
        }
    }
}
