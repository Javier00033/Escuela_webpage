using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EscuelaCore.Migrations
{
    /// <inheritdoc />
    public partial class MejoraDeUsuarios2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Capacidad",
                table: "Aulas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Capacidad",
                table: "Aulas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Aulas",
                keyColumn: "Id",
                keyValue: 1,
                column: "Capacidad",
                value: 5);

            migrationBuilder.UpdateData(
                table: "Aulas",
                keyColumn: "Id",
                keyValue: 2,
                column: "Capacidad",
                value: 5);

            migrationBuilder.UpdateData(
                table: "Aulas",
                keyColumn: "Id",
                keyValue: 3,
                column: "Capacidad",
                value: 5);

            migrationBuilder.UpdateData(
                table: "Aulas",
                keyColumn: "Id",
                keyValue: 4,
                column: "Capacidad",
                value: 5);

            migrationBuilder.UpdateData(
                table: "Aulas",
                keyColumn: "Id",
                keyValue: 5,
                column: "Capacidad",
                value: 5);

            migrationBuilder.UpdateData(
                table: "Aulas",
                keyColumn: "Id",
                keyValue: 6,
                column: "Capacidad",
                value: 5);

            migrationBuilder.UpdateData(
                table: "Aulas",
                keyColumn: "Id",
                keyValue: 7,
                column: "Capacidad",
                value: 5);

            migrationBuilder.UpdateData(
                table: "Aulas",
                keyColumn: "Id",
                keyValue: 8,
                column: "Capacidad",
                value: 5);

            migrationBuilder.UpdateData(
                table: "Aulas",
                keyColumn: "Id",
                keyValue: 9,
                column: "Capacidad",
                value: 5);

            migrationBuilder.UpdateData(
                table: "Aulas",
                keyColumn: "Id",
                keyValue: 10,
                column: "Capacidad",
                value: 5);
        }
    }
}
