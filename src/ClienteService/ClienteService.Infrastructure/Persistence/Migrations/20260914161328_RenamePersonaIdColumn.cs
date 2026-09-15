using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClienteService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenamePersonaIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                table: "personas",
                newName: "persona_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "persona_id",
                table: "personas",
                newName: "id");
        }
    }
}
