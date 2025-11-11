using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EscuelaCore.Models
{
    public class Usuario : IdentityUser
    {
        public bool Activo { get; set; } = true;
    }
}
