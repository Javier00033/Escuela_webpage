using EscuelaCore.Enums;
using EscuelaCore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EscuelaCore.Data
{
    public class EscuelaCoreContext : IdentityDbContext<Usuario, IdentityRole, string>
    {
        public EscuelaCoreContext(DbContextOptions<EscuelaCoreContext> options) : base(options){}

        public DbSet<Alumno> Alumnos => Set<Alumno>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Aula> Aulas => Set<Aula>();
        public DbSet<AulaProfesor> AulaProfesores => Set<AulaProfesor>();
        public DbSet<Curso> Cursos => Set<Curso>();
        public DbSet<Evaluacion> Evaluaciones => Set<Evaluacion>();
        public DbSet<Matricula> Matriculas => Set<Matricula>();
        public DbSet<Profesor> Profesores => Set<Profesor>();
        public DbSet<Traza> Trazas => Set<Traza>();
        public async Task<Curso> GetCursoActualAsync()
            => await Cursos.FirstOrDefaultAsync(c => c.Activo)
                ?? await Cursos.OrderByDescending(c => c.FechaInicio).FirstAsync();      

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Alumno>()
                .Property(al => al.Carrera)
                .HasConversion<string>()
                .HasMaxLength(250);
            modelBuilder.Entity<Aula>()
                .Property(au => au.Carrera)
                .HasConversion<string>()
                .HasMaxLength(250);
            modelBuilder.Entity<AulaProfesor>()
                .Property(ap => ap.Asignatura)
                .HasConversion<string>()
                .HasMaxLength(250);
            modelBuilder.Entity<Evaluacion>()
                .Property(e => e.Asignatura)
                .HasConversion<string>()
                .HasMaxLength(250);
            modelBuilder.Entity<Profesor>()
                .Property(p => p.Asignatura)
                .HasConversion<string>()
                .HasMaxLength(250);

            ConfiguracionAlumnos(modelBuilder);
            ConfiguracionAulas(modelBuilder);
            ConfiguracionEvaluaciones(modelBuilder);
            ConfiguracionProfesores(modelBuilder);
            ConfiguracionMatriculas(modelBuilder);
            ConfiguracionAulaProfesores(modelBuilder);
            ConfiguracionTrazas(modelBuilder);
            ConfiguracionCursos(modelBuilder);
            ConfiguracionDatosIniciales(modelBuilder);
        }

        public void ConfiguracionAlumnos(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Alumno>(entity =>
            {
                entity.HasKey(al => al.Id);
                entity.Property(al => al.Nombre).IsRequired().HasMaxLength(250);
                entity.Property(al => al.Apellidos).IsRequired().HasMaxLength(250);
                entity.Property(al => al.CI).IsRequired().HasMaxLength(11);
                entity.HasIndex(al => al.CI).IsUnique();
                entity.HasIndex(al => new { al.Nombre, al.Apellidos }).IsUnique();
                entity.HasOne(al => al.Aula)
                      .WithMany(au => au.Alumnos)
                      .HasForeignKey(al => al.AulaId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(al => al.Usuario)
                      .WithOne()
                      .HasForeignKey<Alumno>(al => al.UsuarioId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.Property(al => al.Activo).HasDefaultValue(true);
            });
        }

        public void ConfiguracionAulas(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Aula>(entity =>
            {
                entity.HasKey(au => au.Id);
                entity.Property(au => au.Numero).IsRequired();
                entity.HasIndex(au => au.Numero).IsUnique();
                entity.HasMany(au => au.Alumnos)
                      .WithOne(au => au.Aula)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        public void ConfiguracionEvaluaciones(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Evaluacion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nota).IsRequired().HasColumnType("decimal(3,2)");
                entity.Property(e => e.FechaEvaluacion).IsRequired();
                entity.HasOne(e => e.Alumno)
                    .WithMany(al => al.Evaluaciones)
                    .HasForeignKey(e => e.AlumnoId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Profesor)
                    .WithMany(p => p.Evaluaciones)
                    .HasForeignKey(e => e.ProfesorId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Curso)
                    .WithMany(c => c.Evaluaciones)
                    .HasForeignKey(e => e.CursoId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.Editable).HasDefaultValue(true);
            });
        }

        public void ConfiguracionProfesores(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Profesor>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Nombre).IsRequired().HasMaxLength(250);
                entity.Property(p => p.Apellidos).IsRequired().HasMaxLength(250);
                entity.Property(p => p.CI).IsRequired().HasMaxLength(11);
                entity.HasIndex(p => p.CI).IsUnique();
                entity.HasIndex(p => new { p.Nombre, p.Apellidos }).IsUnique();
                entity.HasOne(p => p.Usuario)
                      .WithOne()
                      .HasForeignKey<Profesor>(p => p.UsuarioId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.Property(p => p.Activo).HasDefaultValue(true);
            });
        }

        private void ConfiguracionMatriculas(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Matricula>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.FechaMatricula).IsRequired();
                entity.HasOne(m => m.Alumno)
                    .WithMany(a => a.Matriculas)
                    .HasForeignKey(m => m.AlumnoId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(m => m.Aula)
                    .WithMany(a => a.Matriculas)
                    .HasForeignKey(m => m.AulaId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(m => m.Curso)
                    .WithMany(c => c.Matriculas)
                    .HasForeignKey(m => m.CursoId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(m => new { m.AlumnoId, m.Carrera, m.CursoId })
                    .IsUnique();
            });
        }

        private void ConfiguracionAulaProfesores(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AulaProfesor>(entity =>
            {
                entity.HasKey(ap => ap.Id);
                entity.HasOne(ap => ap.Aula)
                    .WithMany(a => a.AulaProfesores)
                    .HasForeignKey(ap => ap.AulaId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(ap => ap.Profesor)
                    .WithMany(p => p.AulaProfesores)
                    .HasForeignKey(ap => ap.ProfesorId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(ap => ap.Curso)
                    .WithMany(c => c.AulaProfesores)
                    .HasForeignKey(ap => ap.CursoId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(ap => new { ap.AulaId, ap.Asignatura, ap.CursoId })
                    .IsUnique();
            });
        }

        private void ConfiguracionCursos(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Curso>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Nombre).IsRequired().HasMaxLength(250);
                entity.Property(c => c.FechaInicio).IsRequired();
                entity.Property(c => c.FechaFin).IsRequired();
                entity.Property(c => c.Activo).HasDefaultValue(true);
                entity.HasIndex(c => new { c.FechaInicio, c.FechaFin }).IsUnique();
            });
        }

        public void ConfiguracionTrazas(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Traza>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Mensaje).HasColumnType("nvarchar(MAX)").IsRequired();
                entity.Property(t => t.Fecha).IsRequired();
                entity.Property(t => t.Usuario).HasMaxLength(250);
                entity.Property(t => t.Operacion).HasMaxLength(50);
            });
        }

        private void ConfiguracionDatosIniciales(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Curso>().HasData(
                new Curso
                {
                    Id = 1,
                    Nombre = "2024-2025",
                    FechaInicio = new DateTime(2025, 9, 1),
                    FechaFin = new DateTime(2026, 6, 30),
                    Activo = true
                }
            );

            modelBuilder.Entity<Aula>().HasData(
                new Aula { Id = 1, Numero = 1, Carrera = Carrera.Ciencias },
                new Aula { Id = 2, Numero = 2, Carrera = Carrera.Ciencias },
                new Aula { Id = 3, Numero = 3, Carrera = Carrera.Ciencias },
                new Aula { Id = 4, Numero = 4, Carrera = Carrera.Ciencias },
                new Aula { Id = 5, Numero = 5, Carrera = Carrera.Ciencias },
                new Aula { Id = 6, Numero = 6, Carrera = Carrera.Letras },
                new Aula { Id = 7, Numero = 7, Carrera = Carrera.Letras },
                new Aula { Id = 8, Numero = 8, Carrera = Carrera.Letras },
                new Aula { Id = 9, Numero = 9, Carrera = Carrera.Letras },
                new Aula { Id = 10, Numero = 10, Carrera = Carrera.Letras }
            );

            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "1", Name = "Administrador", NormalizedName = "Administrador" },
                new IdentityRole { Id = "2", Name = "Profesor", NormalizedName = "Profesor" },
                new IdentityRole { Id = "3", Name = "Alumno", NormalizedName = "Alumno" }
                );
        }
    }
}
