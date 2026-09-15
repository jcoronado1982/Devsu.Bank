using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CuentaMovimientoService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCuentaMovimientoModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cuentas",
                columns: table => new
                {
                    numero_cuenta = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tipo_cuenta = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    saldo_inicial = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    estado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    cliente_id = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas", x => x.numero_cuenta);
                });

            migrationBuilder.CreateTable(
                name: "movimientos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tipo_movimiento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    saldo = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    numero_cuenta = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos", x => x.id);
                    table.ForeignKey(
                        name: "FK_movimientos_cuentas_numero_cuenta",
                        column: x => x.numero_cuenta,
                        principalTable: "cuentas",
                        principalColumn: "numero_cuenta",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_cliente_id",
                table: "cuentas",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_fecha",
                table: "movimientos",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_numero_cuenta",
                table: "movimientos",
                column: "numero_cuenta");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movimientos");

            migrationBuilder.DropTable(
                name: "cuentas");
        }
    }
}
