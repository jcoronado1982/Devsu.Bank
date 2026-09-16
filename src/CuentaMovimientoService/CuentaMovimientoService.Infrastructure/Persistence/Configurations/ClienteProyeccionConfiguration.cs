using CuentaMovimientoService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CuentaMovimientoService.Infrastructure.Persistence.Configurations;

public class ClienteProyeccionConfiguration : IEntityTypeConfiguration<ClienteProyeccion>
{
    public void Configure(EntityTypeBuilder<ClienteProyeccion> builder)
    {
        builder.ToTable("cliente_proyecciones");

        builder.HasKey(c => c.ClienteId);
        builder.Property(c => c.ClienteId)
            .HasColumnName("cliente_id")
            .ValueGeneratedNever();

        builder.Property(c => c.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Identificacion)
            .HasColumnName("identificacion")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Estado)
            .HasColumnName("estado")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasData(
            new ClienteProyeccion(1, "Jose Lema", "1234567890", true),
            new ClienteProyeccion(2, "Marianela Montalvo", "0975489650", true),
            new ClienteProyeccion(3, "Juan Osorio", "0988745870", true)
        );
    }
}
