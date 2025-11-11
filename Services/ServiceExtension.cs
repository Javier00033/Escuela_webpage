using EscuelaCore.Services.Implementations;
using EscuelaCore.Services.Interfaces;

namespace EscuelaCore.Services
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<ITrazaService, TrazaService>();
            services.AddScoped<IAulaService, AulaService>();
            services.AddScoped<IAlumnoService, AlumnoService>();
            services.AddScoped<IUsuarioService, UsuarioService>();
            return services;
        }
    }
}
