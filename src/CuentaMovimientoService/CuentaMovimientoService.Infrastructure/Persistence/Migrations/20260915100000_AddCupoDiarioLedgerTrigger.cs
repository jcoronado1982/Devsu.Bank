using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CuentaMovimientoService.Infrastructure.Persistence.Migrations
{
    public partial class AddCupoDiarioLedgerTrigger : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION fn_validar_saldo_ledger()
RETURNS TRIGGER AS $$
DECLARE
    v_saldo_inicial NUMERIC(18,2);
    v_saldo_acumulado NUMERIC(18,2);
    v_saldo_anterior NUMERIC(18,2);
    v_retirado_hoy NUMERIC(18,2);
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

    -- EB-03: cupo diario acumulado de retiros, recalculado dentro del mismo lock de fila
    IF NEW.valor < 0 THEN
        SELECT COALESCE(SUM(ABS(valor)), 0.00) INTO v_retirado_hoy
        FROM movimientos
        WHERE numero_cuenta = NEW.numero_cuenta
          AND valor < 0
          AND fecha >= date_trunc('day', NEW.fecha)
          AND fecha < date_trunc('day', NEW.fecha) + interval '1 day';

        IF v_retirado_hoy + ABS(NEW.valor) > 1000.00 THEN
            RAISE EXCEPTION 'Cupo diario Excedido' USING ERRCODE = 'P0001';
        END IF;
    END IF;

    -- Asignar el saldo exacto resultante secuencial acumulado
    -- Si el saldo resultante es negativo, PostgreSQL disparará automáticamente CK_movimientos_saldo
    NEW.saldo := v_saldo_anterior + NEW.valor;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION fn_validar_saldo_ledger()
RETURNS TRIGGER AS $$
DECLARE
    v_saldo_inicial NUMERIC(18,2);
    v_saldo_acumulado NUMERIC(18,2);
    v_saldo_anterior NUMERIC(18,2);
BEGIN
    SELECT saldo_inicial INTO v_saldo_inicial
    FROM cuentas
    WHERE numero_cuenta = NEW.numero_cuenta
    FOR UPDATE;

    IF NOT FOUND THEN
        RETURN NEW;
    END IF;

    SELECT COALESCE(SUM(valor), 0.00) INTO v_saldo_acumulado
    FROM movimientos
    WHERE numero_cuenta = NEW.numero_cuenta;

    v_saldo_anterior := v_saldo_inicial + v_saldo_acumulado;

    NEW.saldo := v_saldo_anterior + NEW.valor;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
");
        }
    }
}
