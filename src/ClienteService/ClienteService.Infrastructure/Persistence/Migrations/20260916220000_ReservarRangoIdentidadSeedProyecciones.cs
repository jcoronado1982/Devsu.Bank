using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClienteService.Infrastructure.Persistence.Migrations
{
    public partial class ReservarRangoIdentidadSeedProyecciones : Migration
    {
        private const long PrimerIdDisponible = 1000;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                SELECT setval(
                    pg_get_serial_sequence('personas', 'persona_id'),
                    GREATEST({PrimerIdDisponible}, COALESCE((SELECT MAX(persona_id) FROM personas), 0) + 1),
                    false);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SELECT setval(
                    pg_get_serial_sequence('personas', 'persona_id'),
                    GREATEST(1, COALESCE((SELECT MAX(persona_id) FROM personas), 0) + 1),
                    false);
            ");
        }
    }
}
