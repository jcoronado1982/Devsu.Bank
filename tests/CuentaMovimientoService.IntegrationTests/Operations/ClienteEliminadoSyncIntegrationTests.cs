using System.Threading.Tasks;
using CuentaMovimientoService.Domain.Entities;
using CuentaMovimientoService.Infrastructure.Adapters;
using CuentaMovimientoService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace CuentaMovimientoService.IntegrationTests.Operations;

/// <summary>
/// Verifica el comportamiento real de sincronizacion cuando ClienteService elimina un
/// cliente: ClienteEliminadoConsumer delega en IClienteInfoPort.DesactivarProyeccionAsync,
/// cuya implementacion real (ClienteExistsPort) se ejercita aqui contra Postgres real.
/// El consumer en si es un pass-through de una linea; lo que importa proteger es que el
/// soft-delete preserva integridad referencial (cuentas/movimientos Append-Only) en vez
/// de borrar la fila.
/// </summary>
public class ClienteEliminadoSyncIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("test_cliente_eliminado")
        .WithUsername("postgres")
        .WithPassword("PrivadoTest01*")
        .Build();

    private CuentaMovimientoDbContext _context = null!;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<CuentaMovimientoDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        _context = new CuentaMovimientoDbContext(options);
        await _context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task DesactivarProyeccionAsync_ConClienteExistente_DebeMarcarEstadoFalso_SinBorrarLaFila()
    {
        // Arrange
        var proyeccion = new ClienteProyeccion(101, "Jose Lema", "0102030405", estado: true);
        _context.ClienteProyecciones.Add(proyeccion);
        await _context.SaveChangesAsync();

        var port = new ClienteExistsPort(_context);

        // Act
        await port.DesactivarProyeccionAsync(101);

        // Assert
        var actualizado = await _context.ClienteProyecciones.FindAsync(101L);
        actualizado.Should().NotBeNull();
        actualizado!.Estado.Should().BeFalse();
        actualizado.Nombre.Should().Be("Jose Lema");
        actualizado.Identificacion.Should().Be("0102030405");
    }

    [Fact]
    public async Task DesactivarProyeccionAsync_ConClienteInexistente_DebeSerNoOp()
    {
        // Arrange
        var port = new ClienteExistsPort(_context);

        // Act
        Func<Task> act = () => port.DesactivarProyeccionAsync(9999);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
