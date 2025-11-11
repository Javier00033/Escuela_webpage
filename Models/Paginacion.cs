using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Models
{
    public class Paginacion<T>
    {
        public int PaginaActual { get; set; }
        public int RegistrosPorPagina { get; set; }
        public int TotalDePaginas { get; set; }
        public int TotalDeRegistros { get; set; }
        public List<T> Datos { get; set; }
    }
}
