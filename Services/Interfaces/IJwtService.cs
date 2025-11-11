using System.Security.Claims;

namespace EscuelaCore.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateJwtToken(List<Claim> claims);
        ClaimsPrincipal ValidateJwtToken(string token);
    }
}
