using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814

namespace CuentaMovimientoService.Infrastructure.Persistence.Migrations
{
    public partial class AddClienteProyeccionAndLedgerTrigger : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cliente_proyecciones",
                columns: table => new
                {
                    cliente_id = table.Column<long>(type: "bigint", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    identificacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    estado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cliente_proyecciones", x => x.cliente_id);
                });

            migrationBuilder.InsertData(
                table: "cliente_proyecciones",
                columns: new[] { "cliente_id", "estado", "identificacion", "nombre" },
                values: new object[,]
                {
                    { 1L, true, "1234567890", "Jose Lema" },
                    { 2L, true, "0975489650", "Marianela Montalvo" },
                    { 3L, true, "0988745870", "Juan Osorio" }
                });

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION fn_validar_saldo_ledger()
RETURNS TRIGGER AS $$
DECLARE
    v_saldo_inicial NUMERIC(18,2);
    v_saldo_acumulado NUMERIC(18,2);
    v_saldo_anterior NUMERIC(18,2);
BEGIN
    -- Bloquear la fila de la cuenta para serializar transacciones concurrentes (EB-08)
    SELECT saldo_inicial INTO v_saldo_inicial
    FROM cuentas
    WHERE numero_cuenta = NEW.numero_cuenta
    FOR UPDATE;

    IF NOT FOUND THEN
        RETURN NEW;
    END IF;

    -- Obtener el saldo acumulado antes de este nuevo movimiento
    SELECT COALESCE(SUM(valor), 0.00) INTO v_saldo_acumulado
    FROM movimientos
    WHERE numero_cuenta = NEW.numero_cuenta;

    v_saldo_anterior := v_saldo_inicial + v_saldo_acumulado;

    -- Asignar el saldo exacto resultante secuencial acumulado
    -- Si el saldo resultante es negativo, PostgreSQL disparará automáticamente CK_movimientos_saldo
    NEW.saldo := v_saldo_anterior + NEW.valor;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_validar_saldo_ledger ON movimientos;
CREATE TRIGGER trg_validar_saldo_ledger
BEFORE INSERT ON movimientos
FOR EACH ROW
EXECUTE FUNCTION fn_validar_saldo_ledger();
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS trg_validar_saldo_ledger ON movimientos;
DROP FUNCTION IF EXISTS fn_validar_saldo_ledger();
");

            migrationBuilder.DropTable(
                name: "cliente_proyecciones");
        }
    }
}
