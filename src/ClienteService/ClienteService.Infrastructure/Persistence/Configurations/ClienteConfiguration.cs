using ClienteService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClienteService.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo Fluent API de Cliente (tabla "clientes", hija TPT de "personas"). Vive en
/// Infrastructure y no en Domain: la entidad Cliente no conoce nombres de columna, tipos de
/// base de datos ni check constraints; todo el conocimiento de PostgreSQL se concentra aquí.
/// </summary>
public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        // Estrategia Table-per-Type (TPT): clientes tiene su propia tabla y hereda la PK de personas
        builder.ToTable("clientes", tb =>
        {
            // La columna PK de "clientes" se llama cliente_id, aunque en C# reutiliza PersonaId
            // (heredado de Persona) como su valor: es la misma clave primaria en ambas tablas.
            tb.Property(c => c.PersonaId).HasColumnName("cliente_id");
            // Chequeo redundante con la validación de dominio en Cliente.CambiarContrasena
            // (que ya exige no-vacío): esta constraint es la última línea de defensa a nivel de
            // base de datos contra un hash BCrypt corrupto o truncado (mínimo 4 caracteres).
            tb.HasCheckConstraint("CK_clientes_contrasena", "length(trim(contrasena)) >= 4");
        });

        // ClienteId es una propiedad calculada (=> Id) que no debe mapearse como columna propia;
        // la columna real es PersonaId, renombrada arriba a "cliente_id".
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
