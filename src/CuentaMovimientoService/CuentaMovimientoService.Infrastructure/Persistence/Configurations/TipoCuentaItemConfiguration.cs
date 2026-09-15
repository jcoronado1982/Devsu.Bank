using CuentaMovimientoService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CuentaMovimientoService.Infrastructure.Persistence.Configurations;

public class TipoCuentaItemConfiguration : IEntityTypeConfiguration<TipoCuentaItem>
{
    public void Configure(EntityTypeBuilder<TipoCuentaItem> builder)
    {
        builder.ToTable("tipos_cuenta");

        builder.HasKey(t => t.TipoCuentaId);
        builder.Property(t => t.TipoCuentaId)
            .HasColumnName("tipo_cuenta_id");

        builder.Property(t => t.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(10)
            .IsRequired();

        builder.HasIndex(t => t.Codigo)
            .IsUnique();

        builder.Property(t => t.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(t => t.Nombre)
            .IsUnique();

        // Datos semilla oficiales del catálogo bancario
        builder.HasData(
            new TipoCuentaItem(1, "AHORRO", "Ahorros"),
            new TipoCuentaItem(2, "CORRIENTE", "Corriente")
        );
    }
}
