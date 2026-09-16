using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClienteService.Infrastructure.Persistence.Migrations
{
    public partial class RenameClienteIdColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_clientes_personas_id",
                table: "clientes");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "clientes",
                newName: "cliente_id");

            migrationBuilder.AddForeignKey(
                name: "FK_clientes_personas_cliente_id",
                table: "clientes",
                column: "cliente_id",
                principalTable: "personas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_clientes_personas_cliente_id",
                table: "clientes");

            migrationBuilder.RenameColumn(
                name: "cliente_id",
                table: "clientes",
                newName: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_clientes_personas_id",
                table: "clientes",
                column: "id",
                principalTable: "personas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
