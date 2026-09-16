using ClienteService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClienteService.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ClienteDbContext))]
    [Migration("20260914152639_AddDataIntegrityCheckConstraints")]
    partial class AddDataIntegrityCheckConstraints
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ClienteService.Domain.Entities.Persona", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("Direccion")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("direccion");

                    b.Property<short>("Edad")
                        .HasColumnType("smallint")
                        .HasColumnName("edad");

                    b.Property<string>("Genero")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("genero");

                    b.Property<string>("Identificacion")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("identificacion");

                    b.Property<string>("Nombre")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("nombre");

                    b.Property<string>("Telefono")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("telefono");

                    b.Property<uint>("xmin")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("xid")
                        .HasColumnName("xmin");

                    b.HasKey("Id");

                    b.HasIndex("Identificacion")
                        .IsUnique();

                    b.ToTable("personas", null, t =>
                        {
                            t.HasCheckConstraint("CK_personas_edad", "edad >= 0 AND edad <= 120");

                            t.HasCheckConstraint("CK_personas_identificacion", "length(trim(identificacion)) >= 3");

                            t.HasCheckConstraint("CK_personas_nombre", "length(trim(nombre)) >= 2");
                        });

                    b.UseTptMappingStrategy();
                });

            modelBuilder.Entity("ClienteService.Domain.Entities.Cliente", b =>
                {
                    b.HasBaseType("ClienteService.Domain.Entities.Persona");

                    b.Property<string>("Contrasena")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("contrasena");

                    b.Property<bool>("Estado")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("estado");

                    b.ToTable("clientes", null, t =>
                        {
                            t.HasCheckConstraint("CK_personas_edad", "edad >= 0 AND edad <= 120");

                            t.HasCheckConstraint("CK_personas_identificacion", "length(trim(identificacion)) >= 3");

                            t.HasCheckConstraint("CK_personas_nombre", "length(trim(nombre)) >= 2");

                            t.HasCheckConstraint("CK_clientes_contrasena", "length(trim(contrasena)) >= 4");

                            t.Property("Id")
                                .HasColumnName("cliente_id");
                        });
                });

            modelBuilder.Entity("ClienteService.Domain.Entities.Cliente", b =>
                {
                    b.HasOne("ClienteService.Domain.Entities.Persona", null)
                        .WithOne()
                        .HasForeignKey("ClienteService.Domain.Entities.Cliente", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
