using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClienteService.Infrastructure.Persistence.Migrations
{
    public partial class AddDataIntegrityCheckConstraints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<short>(
                name: "edad",
                table: "personas",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddCheckConstraint(
                name: "CK_personas_edad",
                table: "personas",
                sql: "edad >= 0 AND edad <= 120");

            migrationBuilder.AddCheckConstraint(
                name: "CK_personas_identificacion",
                table: "personas",
                sql: "length(trim(identificacion)) >= 3");

            migrationBuilder.AddCheckConstraint(
                name: "CK_personas_nombre",
                table: "personas",
                sql: "length(trim(nombre)) >= 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_clientes_contrasena",
                table: "clientes",
                sql: "length(trim(contrasena)) >= 4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_personas_edad",
                table: "personas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_personas_identificacion",
                table: "personas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_personas_nombre",
                table: "personas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_clientes_contrasena",
                table: "clientes");

            migrationBuilder.AlterColumn<int>(
                name: "edad",
                table: "personas",
                type: "integer",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");
        }
    }
}
