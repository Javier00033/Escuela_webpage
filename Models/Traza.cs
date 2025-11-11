using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EscuelaCore.Models
{
    public class Traza
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(MAX)")]
        public string Mensaje { get; set; } = null!;

        [Required]
        public string Operacion { get; set; }

        [Required, MaxLength(250)]
        public DateTime Fecha { get; set; }

        [Required, MaxLength(250)]
        public string? Usuario { get; set; }
    }
}
