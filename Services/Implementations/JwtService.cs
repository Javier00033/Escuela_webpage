using EscuelaCore.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EscuelaCore.Services.Implementations
{
    /// <summary>
    /// Servicio para generación y validación de tokens JWT.
    /// </summary>
    public class JwtService : IJwtService
    {
        public readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Genera un token JWT a partir de una lista de claims.
        /// </summary>
        /// <param name="claims">Lista de claims para el token.</param>
        /// <returns>Token JWT generado.</returns>
        public string GenerateJwtToken(List<Claim> claims)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var llaveSecreta = jwtSettings["llaveSecreta"];
            var issuer = jwtSettings["issuer"];
            var audience = jwtSettings["audience"];

            if (string.IsNullOrEmpty(llaveSecreta))
                throw new ArgumentException("JWT llave secreta no configurada");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(llaveSecreta));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Valida un token JWT y retorna los claims del usuario.
        /// </summary>
        /// <param name="token">Token JWT a validar.</param>
        /// <returns>ClaimsPrincipal con los claims del usuario.</returns>
        public ClaimsPrincipal ValidateJwtToken(string token)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var llaveSecreta = jwtSettings["llaveSecreta"];
            var issuer = jwtSettings["issuer"];
            var audience = jwtSettings["audience"];

            var tokenHandler =new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(llaveSecreta ?? "Desconocida");

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            return tokenHandler.ValidateToken(token, validationParameters, out _);
        }
    }
}
