using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EscuelaCore.Migrations
{
    /// <inheritdoc />
    public partial class includeCursosIdMatricula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matriculas_AlumnoId_Carrera",
                table: "Matriculas");

            migrationBuilder.CreateIndex(
                name: "IX_Matriculas_AlumnoId_Carrera_CursoId",
                table: "Matriculas",
                columns: new[] { "AlumnoId", "Carrera", "CursoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matriculas_AlumnoId_Carrera_CursoId",
                table: "Matriculas");

            migrationBuilder.CreateIndex(
                name: "IX_Matriculas_AlumnoId_Carrera",
                table: "Matriculas",
                columns: new[] { "AlumnoId", "Carrera" },
                unique: true);
        }
    }
}
