using CuentaMovimientoService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CuentaMovimientoService.Infrastructure.Persistence.Configurations;

public class MovimientoConfiguration : IEntityTypeConfiguration<Movimiento>
{
    public void Configure(EntityTypeBuilder<Movimiento> builder)
    {
        builder.ToTable("movimientos", tb =>
        {
            tb.HasCheckConstraint("CK_movimientos_valor", "valor <> 0");
            tb.HasCheckConstraint("CK_movimientos_saldo", "saldo >= 0");
            // Acepta "Deposito" y "Depósito" (con y sin tilde): el dominio genera el valor sin
            // tilde (MovimientoService), pero el constraint tolera ambas variantes por si se
            // inserta directamente con acento (seeds, scripts manuales).
            tb.HasCheckConstraint("CK_movimientos_tipo", "tipo_movimiento IN ('Deposito', 'Retiro', 'Depósito')");
        });

        builder.HasKey(m => m.MovimientoId);
        builder.Property(m => m.MovimientoId)
            .HasColumnName("movimiento_id")
            .ValueGeneratedOnAdd();

        builder.Ignore(m => m.Id);

        builder.Property(m => m.Fecha)
            .HasColumnName("fecha")
            .IsRequired();

        builder.Property(m => m.TipoMovimiento)
            .HasColumnName("tipo_movimiento")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.Valor)
            .HasColumnName("valor")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(m => m.Saldo)
            .HasColumnName("saldo")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(m => m.NumeroCuenta)
            .HasColumnName("numero_cuenta")
            .HasMaxLength(34) // Estándar Internacional ISO 13616 (IBAN) / NACHA / CBU
            .IsRequired();

        builder.HasIndex(m => m.NumeroCuenta);
        builder.HasIndex(m => m.Fecha);
    }
}
