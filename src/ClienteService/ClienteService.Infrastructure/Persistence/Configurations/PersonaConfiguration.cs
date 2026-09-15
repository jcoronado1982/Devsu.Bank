using ClienteService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClienteService.Infrastructure.Persistence.Configurations;

public class PersonaConfiguration : IEntityTypeConfiguration<Persona>
{
    public void Configure(EntityTypeBuilder<Persona> builder)
    {
        builder.ToTable("personas", tb =>
        {
            tb.HasCheckConstraint("CK_personas_edad", "edad >= 0 AND edad <= 120");
            tb.HasCheckConstraint("CK_personas_nombre", "length(trim(nombre)) >= 2");
            tb.HasCheckConstraint("CK_personas_identificacion", "length(trim(identificacion)) >= 3");
        });

        builder.HasKey(p => p.PersonaId);
        builder.Property(p => p.PersonaId)
            .HasColumnName("persona_id")
            .ValueGeneratedOnAdd();

        builder.Ignore(p => p.Id);

        builder.Property(p => p.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Genero)
            .HasColumnName("genero")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Edad)
            .HasColumnName("edad")
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(p => p.Identificacion)
            .HasColumnName("identificacion")
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(p => p.Identificacion)
            .IsUnique();

        builder.Property(p => p.Direccion)
            .HasColumnName("direccion")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.Telefono)
            .HasColumnName("telefono")
            .HasMaxLength(20)
            .IsRequired();

        // Control de concurrencia optimista nativo de PostgreSQL
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsRowVersion();
    }
}
