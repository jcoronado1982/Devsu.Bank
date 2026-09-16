using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuentaMovimientoService.Infrastructure.Persistence.Migrations
{
    public partial class AddBankingCheckConstraints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_movimientos_saldo",
                table: "movimientos",
                sql: "saldo >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_movimientos_tipo",
                table: "movimientos",
                sql: "tipo_movimiento IN ('Deposito', 'Retiro', 'Depósito')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_movimientos_valor",
                table: "movimientos",
                sql: "valor <> 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cuentas_numero_cuenta",
                table: "cuentas",
                sql: "length(trim(numero_cuenta)) >= 4");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cuentas_saldo_inicial",
                table: "cuentas",
                sql: "saldo_inicial >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cuentas_tipo_cuenta",
                table: "cuentas",
                sql: "tipo_cuenta IN ('Ahorros', 'Corriente')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_movimientos_saldo",
                table: "movimientos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_movimientos_tipo",
                table: "movimientos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_movimientos_valor",
                table: "movimientos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_cuentas_numero_cuenta",
                table: "cuentas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_cuentas_saldo_inicial",
                table: "cuentas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_cuentas_tipo_cuenta",
                table: "cuentas");
        }
    }
}
