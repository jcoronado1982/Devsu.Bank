using ClienteService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClienteService.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo Fluent API de Persona (tabla "personas", raíz de la jerarquía TPT). Los check
/// constraints aquí declarados son deliberadamente más permisivos que las validaciones de
/// Persona.cs (p. ej. edad >= 0 en vez de >= 18): actúan como red de seguridad a nivel de base
/// de datos contra datos insertados fuera del dominio (migraciones, scripts, otra app), no
/// como la fuente de verdad de las reglas de negocio.
/// </summary>
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

        // Respaldo físico de la regla de negocio EB-06 (identificación duplicada -> 409):
        // este índice único es la garantía definitiva de no-duplicados a nivel de base de
        // datos, incluso ante condiciones de carrera que el chequeo previo en memoria no cubre.
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

        // Control de concurrencia optimista nativo de PostgreSQL: xmin es la columna de sistema
        // que PostgreSQL incrementa en cada UPDATE de la fila; EF Core la usa como token de
        // concurrencia (IsRowVersion) para detectar ediciones concurrentes sin añadir una
        // columna de versión propia.
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsRowVersion();
    }
}
