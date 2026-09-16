using System.Threading.Tasks;
using CuentaMovimientoService.Domain.Entities;
using CuentaMovimientoService.Infrastructure.Adapters;
using CuentaMovimientoService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace CuentaMovimientoService.IntegrationTests.Database;

/// <summary>
/// Documenta, de forma ejecutable, una brecha de consistencia real entre microservicios
/// encontrada al probar manualmente la API: ClienteExistsPort.ExisteClienteAsync no filtraba
/// por Estado, así que un cliente eliminado (soft-delete vía ClienteEliminadoEvent, que marca
/// Estado=false para preservar el historial de cuentas/movimientos) seguía "existiendo" para
/// efectos de abrir cuentas NUEVAS. Usa Postgres real vía Testcontainers porque el bug vive en
/// el adaptador de Infrastructure (consulta EF Core), no en el dominio.
/// </summary>
public class ClienteExistsPortGapsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("test_cliente_exists_gap")
        .WithUsername("postgres")
        .WithPassword("GapsTest01Pass")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<CuentaMovimientoDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;
        await using var context = new CuentaMovimientoDbContext(options);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgresContainer.DisposeAsync();

    private CuentaMovimientoDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<CuentaMovimientoDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;
        return new CuentaMovimientoDbContext(options);
    }

    [Fact]
    public async Task ExisteClienteAsync_ConClienteEliminado_DebeRetornarFalse()
    {
        // Arrange — proyección de un cliente que YA fue eliminado (Estado=false),
        // igual que deja ClienteEliminadoConsumer tras un ClienteEliminadoEvent real.
        await using (var seedContext = CrearContexto())
        {
            seedContext.ClienteProyecciones.Add(new ClienteProyeccion(99, "Cliente Eliminado", "9999999999", estado: false));
            await seedContext.SaveChangesAsync();
        }

        await using var context = CrearContexto();
        var port = new ClienteExistsPort(context);

        // Act
        var existe = await port.ExisteClienteAsync(99);

        // Assert — un cliente eliminado no debe permitir abrir cuentas nuevas
        existe.Should().BeFalse("un cliente con Estado=false fue eliminado y no debería habilitar nuevas cuentas");
    }

    [Fact]
    public async Task ExisteClienteAsync_ConClienteActivo_DebeRetornarTrue()
    {
        // Arrange
        await using (var seedContext = CrearContexto())
        {
            seedContext.ClienteProyecciones.Add(new ClienteProyeccion(100, "Cliente Activo", "8888888888", estado: true));
            await seedContext.SaveChangesAsync();
        }

        await using var context = CrearContexto();
        var port = new ClienteExistsPort(context);

        // Act
        var existe = await port.ExisteClienteAsync(100);

        // Assert
        existe.Should().BeTrue();
    }
}
