using System;
using CuentaMovimientoService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CuentaMovimientoService.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(CuentaMovimientoDbContext))]
    [Migration("20260914161407_RenameMovimientoIdColumn")]
    partial class RenameMovimientoIdColumn
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("CuentaMovimientoService.Domain.Entities.Cuenta", b =>
                {
                    b.Property<string>("NumeroCuenta")
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("numero_cuenta");

                    b.Property<long>("ClienteId")
                        .HasColumnType("bigint")
                        .HasColumnName("cliente_id");

                    b.Property<bool>("Estado")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("estado");

                    b.Property<decimal>("SaldoInicial")
                        .HasColumnType("numeric(18,2)")
                        .HasColumnName("saldo_inicial");

                    b.Property<string>("TipoCuenta")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("tipo_cuenta");

                    b.Property<uint>("xmin")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("NumeroCuenta");

                    b.HasIndex("ClienteId");

                    b.ToTable("cuentas", null, t =>
                        {
                            t.HasCheckConstraint("CK_cuentas_numero_cuenta", "length(trim(numero_cuenta)) >= 4");

                            t.HasCheckConstraint("CK_cuentas_saldo_inicial", "saldo_inicial >= 0");

                            t.HasCheckConstraint("CK_cuentas_tipo_cuenta", "tipo_cuenta IN ('Ahorros', 'Corriente')");
                        });
                });

            modelBuilder.Entity("CuentaMovimientoService.Domain.Entities.Movimiento", b =>
                {
                    b.Property<long>("MovimientoId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("movimiento_id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("MovimientoId"));

                    b.Property<DateTime>("Fecha")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("fecha");

                    b.Property<string>("NumeroCuenta")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("numero_cuenta");

                    b.Property<decimal>("Saldo")
                        .HasColumnType("numeric(18,2)")
                        .HasColumnName("saldo");

                    b.Property<string>("TipoMovimiento")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("tipo_movimiento");

                    b.Property<decimal>("Valor")
                        .HasColumnType("numeric(18,2)")
                        .HasColumnName("valor");

                    b.HasKey("MovimientoId");

                    b.HasIndex("Fecha");

                    b.HasIndex("NumeroCuenta");

                    b.ToTable("movimientos", null, t =>
                        {
                            t.HasCheckConstraint("CK_movimientos_saldo", "saldo >= 0");

                            t.HasCheckConstraint("CK_movimientos_tipo", "tipo_movimiento IN ('Deposito', 'Retiro', 'Depósito')");

                            t.HasCheckConstraint("CK_movimientos_valor", "valor <> 0");
                        });
                });

            modelBuilder.Entity("CuentaMovimientoService.Domain.Entities.Movimiento", b =>
                {
                    b.HasOne("CuentaMovimientoService.Domain.Entities.Cuenta", "Cuenta")
                        .WithMany("Movimientos")
                        .HasForeignKey("NumeroCuenta")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Cuenta");
                });

            modelBuilder.Entity("CuentaMovimientoService.Domain.Entities.Cuenta", b =>
                {
                    b.Navigation("Movimientos");
                });
#pragma warning restore 612, 618
        }
    }
}
