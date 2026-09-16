using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuentaMovimientoService.Infrastructure.Persistence.Migrations
{
    public partial class RenameMovimientoIdColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                table: "movimientos",
                newName: "movimiento_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "movimiento_id",
                table: "movimientos",
                newName: "id");
        }
    }
}
