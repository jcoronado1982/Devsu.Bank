using ClienteService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClienteService.Infrastructure.Persistence.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes", tb =>
        {
            tb.Property(c => c.PersonaId).HasColumnName("cliente_id");
            tb.HasCheckConstraint("CK_clientes_contrasena", "length(trim(contrasena)) >= 4");
        });

        builder.Ignore(c => c.ClienteId);

        builder.Property(c => c.Contrasena)
            .HasColumnName("contrasena")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.Estado)
            .HasColumnName("estado")
            .HasDefaultValue(true)
            .IsRequired();
    }
}
