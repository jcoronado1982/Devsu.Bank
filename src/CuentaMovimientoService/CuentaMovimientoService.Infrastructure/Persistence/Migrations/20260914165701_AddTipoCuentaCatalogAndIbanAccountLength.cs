using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814

namespace CuentaMovimientoService.Infrastructure.Persistence.Migrations
{
    public partial class AddTipoCuentaCatalogAndIbanAccountLength : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_cuentas_tipo_cuenta",
                table: "cuentas");

            migrationBuilder.DropColumn(
                name: "tipo_cuenta",
                table: "cuentas");

            migrationBuilder.AlterColumn<string>(
                name: "numero_cuenta",
                table: "movimientos",
                type: "character varying(34)",
                maxLength: 34,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "numero_cuenta",
                table: "cuentas",
                type: "character varying(34)",
                maxLength: 34,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<short>(
                name: "tipo_cuenta_id",
                table: "cuentas",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.CreateTable(
                name: "tipos_cuenta",
                columns: table => new
                {
                    tipo_cuenta_id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    nombre = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_cuenta", x => x.tipo_cuenta_id);
                });

            migrationBuilder.InsertData(
                table: "tipos_cuenta",
                columns: new[] { "tipo_cuenta_id", "codigo", "nombre" },
                values: new object[,]
                {
                    { (short)1, "AHORRO", "Ahorros" },
                    { (short)2, "CORRIENTE", "Corriente" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_tipo_cuenta_id",
                table: "cuentas",
                column: "tipo_cuenta_id");

            migrationBuilder.CreateIndex(
                name: "IX_tipos_cuenta_codigo",
                table: "tipos_cuenta",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tipos_cuenta_nombre",
                table: "tipos_cuenta",
                column: "nombre",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_cuentas_tipos_cuenta_tipo_cuenta_id",
                table: "cuentas",
                column: "tipo_cuenta_id",
                principalTable: "tipos_cuenta",
                principalColumn: "tipo_cuenta_id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cuentas_tipos_cuenta_tipo_cuenta_id",
                table: "cuentas");

            migrationBuilder.DropTable(
                name: "tipos_cuenta");

            migrationBuilder.DropIndex(
                name: "IX_cuentas_tipo_cuenta_id",
                table: "cuentas");

            migrationBuilder.DropColumn(
                name: "tipo_cuenta_id",
                table: "cuentas");

            migrationBuilder.AlterColumn<string>(
                name: "numero_cuenta",
                table: "movimientos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(34)",
                oldMaxLength: 34);

            migrationBuilder.AlterColumn<string>(
                name: "numero_cuenta",
                table: "cuentas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(34)",
                oldMaxLength: 34);

            migrationBuilder.AddColumn<string>(
                name: "tipo_cuenta",
                table: "cuentas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cuentas_tipo_cuenta",
                table: "cuentas",
                sql: "tipo_cuenta IN ('Ahorros', 'Corriente')");
        }
    }
}
