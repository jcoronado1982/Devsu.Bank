using CuentaMovimientoService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CuentaMovimientoService.Infrastructure.Persistence.Configurations;

public class CuentaConfiguration : IEntityTypeConfiguration<Cuenta>
{
    public void Configure(EntityTypeBuilder<Cuenta> builder)
    {
        builder.ToTable("cuentas", tb =>
        {
            tb.HasCheckConstraint("CK_cuentas_saldo_inicial", "saldo_inicial >= 0");
            tb.HasCheckConstraint("CK_cuentas_numero_cuenta", "length(trim(numero_cuenta)) >= 4");
        });

        builder.HasKey(c => c.NumeroCuenta);
        builder.Property(c => c.NumeroCuenta)
            .HasColumnName("numero_cuenta")
            .HasMaxLength(34) // Estándar Internacional ISO 13616 (IBAN) / NACHA / CBU
            .IsRequired();

        builder.Property(c => c.TipoCuentaId)
            .HasColumnName("tipo_cuenta_id")
            .IsRequired();

        builder.HasOne(c => c.TipoCuentaItem)
            .WithMany()
            .HasForeignKey(c => c.TipoCuentaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(c => c.TipoCuenta);

        builder.Property(c => c.SaldoInicial)
            .HasColumnName("saldo_inicial")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(c => c.Estado)
            .HasColumnName("estado")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(c => c.ClienteId)
            .HasColumnName("cliente_id")
            .IsRequired();

        builder.HasIndex(c => c.ClienteId);

        // Configuración de la relación 1 a Muchos con Movimientos
        builder.HasMany(c => c.Movimientos)
            .WithOne(m => m.Cuenta)
            .HasForeignKey(m => m.NumeroCuenta)
            .OnDelete(DeleteBehavior.Restrict);

        // Control de concurrencia optimista nativo de PostgreSQL para prevenir sobregiros en carreras
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsRowVersion();
    }
}
