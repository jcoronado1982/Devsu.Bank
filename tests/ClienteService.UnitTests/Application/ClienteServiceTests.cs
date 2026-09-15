using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClienteService.Application.DTOs;
using ClienteService.Application.Events;
using ClienteService.Application.Exceptions;
using ClienteService.Application.Ports;
using ClienteService.Application.Services;
using ClienteService.Domain.Entities;
using FluentAssertions;
using Xunit;
using RealClienteService = ClienteService.Application.Services.ClienteService;

namespace ClienteService.UnitTests.Application;

// ─── FAKES DE PUERTOS (dependencias del servicio real, no del servicio en sí) ──
public class FakeClienteRepository : IClienteRepository
{
    private readonly List<Cliente> _store = new();
    private long _nextId = 1;

    public Task<Cliente?> ObtenerPorIdAsync(long clienteId)
        => Task.FromResult(_store.FirstOrDefault(c => c.PersonaId == clienteId));

    public Task<Cliente?> ObtenerPorIdentificacionAsync(string identificacion)
        => Task.FromResult(_store.FirstOrDefault(c => c.Identificacion == identificacion));

    public Task<IEnumerable<Cliente>> ObtenerTodosAsync()
        => Task.FromResult<IEnumerable<Cliente>>(_store.ToList());

    public Task AddAsync(Cliente cliente)
    {
        // Simula la asignación del Id por la base de datos via reflexión
        typeof(ClienteService.Domain.Entities.Persona)
            .GetProperty("PersonaId")!
            .SetValue(cliente, _nextId++);
        _store.Add(cliente);
        return Task.CompletedTask;
    }

    public void Remove(Cliente cliente) => _store.Remove(cliente);

    public Task SaveChangesAsync() => Task.CompletedTask;
}

// Fake determinístico del puerto IPasswordHasher (no es BCrypt real, pero permite
// verificar mecánicamente que el servicio SIEMPRE hashea antes de persistir).
public class FakePasswordHasher : IPasswordHasher
{
    private const string Prefix = "HASH:";

    public string HashPassword(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            throw new ArgumentException("La contraseña a hashear no puede estar vacía.", nameof(plainPassword));

        return Prefix + plainPassword;
    }

    public bool VerifyPassword(string plainPassword, string passwordHash)
        => passwordHash == Prefix + plainPassword;
}

public class FakeUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
}

public class FakeEventBus : IEventBus
{
    public readonly List<object> EventosPublicados = new();

    public Task PublicarAsync<TEvent>(TEvent evento, CancellationToken ct = default) where TEvent : class
    {
        EventosPublicados.Add(evento);
        return Task.CompletedTask;
    }
}

// ─── TESTS (contra el servicio REAL: ClienteService.Application.Services.ClienteService) ──
public class ClienteServiceTests
{
    private static (IClienteService svc, FakeClienteRepository repo, FakePasswordHasher hasher) CrearServicio()
    {
        var repo = new FakeClienteRepository();
        var hasher = new FakePasswordHasher();
        return (new RealClienteService(repo, hasher, new FakeUnitOfWork()), repo, hasher);
    }

    private static CrearClienteDto JoseLemaDto() => new(
        "Jose Lema", "Masculino", 35, "0102030405",
        "Otavalo sn y principal", "098254785", "1234");

    private static CrearClienteDto MarianelaDto() => new(
        "Marianela Montalvo", "Femenino", 28, "0102030406",
        "Amazonas y NNUU", "097548965", "5678");

    // ── Test 1: Creación exitosa ──────────────────────────────────────────
    [Fact]
    public async Task CrearCliente_ConDatosValidos_DebeRetornarClienteConId()
    {
        // Arrange
        var (svc, _, _) = CrearServicio();

        // Act
        var resultado = await svc.CrearClienteAsync(JoseLemaDto());

        // Assert
        resultado.Nombre.Should().Be("Jose Lema");
        resultado.Identificacion.Should().Be("0102030405");
        resultado.Estado.Should().BeTrue();
        resultado.ClienteId.Should().BeGreaterThan(0);
    }

    // ── Seguridad: la contraseña jamás se persiste en texto plano ─────────
    [Fact]
    public async Task CrearCliente_ConDatosValidos_DebePersistirContrasenaHasheada_NuncaEnTextoPlano()
    {
        // Arrange
        var (svc, _, hasher) = CrearServicio();

        // Act
        var resultado = await svc.CrearClienteAsync(JoseLemaDto());

        // Assert — Pilar 1 SEGURIDAD_Y_PROTECCION_DATOS.md: cero texto plano
        resultado.Contrasena.Should().NotBe("1234");
        hasher.VerifyPassword("1234", resultado.Contrasena).Should().BeTrue();
    }

    // ── Test 2: EB-06 Identificación duplicada ────────────────────────────
    [Fact]
    public async Task CrearCliente_ConIdentificacionExistente_DebeLanzarIdentificacionDuplicadaException()
    {
        // Arrange
        var (svc, _, _) = CrearServicio();
        await svc.CrearClienteAsync(JoseLemaDto()); // primer registro

        // Act
        Func<Task> act = () => svc.CrearClienteAsync(JoseLemaDto()); // misma identificación

        // Assert
        await act.Should().ThrowAsync<IdentificacionDuplicadaException>()
            .WithMessage("*0102030405*");
    }

    // ── Test 3: Obtener por id existente ──────────────────────────────────
    [Fact]
    public async Task ObtenerCliente_PorIdExistente_DebeRetornarCliente()
    {
        // Arrange
        var (svc, _, _) = CrearServicio();
        var creado = await svc.CrearClienteAsync(MarianelaDto());

        // Act
        var resultado = await svc.ObtenerClientePorIdAsync(creado.ClienteId);

        // Assert
        resultado.Should().NotBeNull();
        resultado!.Nombre.Should().Be("Marianela Montalvo");
    }

    // ── Test 4: Obtener por id inexistente ────────────────────────────────
    [Fact]
    public async Task ObtenerCliente_PorIdInexistente_DebeRetornarNull()
    {
        // Arrange
        var (svc, _, _) = CrearServicio();

        // Act
        var resultado = await svc.ObtenerClientePorIdAsync(9999);

        // Assert
        resultado.Should().BeNull();
    }

    // ── Test 5: Actualización exitosa ─────────────────────────────────────
    [Fact]
    public async Task ActualizarCliente_ConDatosValidos_DebeActualizarCampos()
    {
        // Arrange
        var (svc, _, hasher) = CrearServicio();
        var creado = await svc.CrearClienteAsync(JoseLemaDto());
        var actualizarDto = new ActualizarClienteDto(
            "Jose Lema Actualizado", "Masculino", 36,
            "Nueva Direccion", "098254785", "4321", Estado: true);

        // Act
        var resultado = await svc.ActualizarClienteAsync(creado.ClienteId, actualizarDto);

        // Assert
        resultado.Nombre.Should().Be("Jose Lema Actualizado");
        resultado.Edad.Should().Be(36);
        resultado.Contrasena.Should().NotBe("4321", "la contraseña nueva también debe hashearse, nunca en texto plano");
        hasher.VerifyPassword("4321", resultado.Contrasena).Should().BeTrue();
    }

    // ── Regresión: identificación es inmutable tras actualizar (DICCIONARIO_DE_DATOS_Y_TIPOS.md) ──
    [Fact]
    public async Task ActualizarCliente_DespuesDeActualizar_LaIdentificacionOriginalNoDebeCambiar()
    {
        // Arrange
        var (svc, _, _) = CrearServicio();
        var creado = await svc.CrearClienteAsync(JoseLemaDto());
        var identificacionOriginal = creado.Identificacion;
        var actualizarDto = new ActualizarClienteDto(
            "Jose Lema Actualizado", "Masculino", 36,
            "Nueva Direccion", "098254785", "4321", Estado: true);

        // Act — ActualizarClienteDto ni siquiera expone un campo de identificación:
        // esta prueba fija el invariante para que una futura reintroducción del campo no lo rompa.
        var resultado = await svc.ActualizarClienteAsync(creado.ClienteId, actualizarDto);

        // Assert
        resultado.Identificacion.Should().Be(identificacionOriginal,
            "la identificación es inmutable una vez creado el cliente");
    }

    // ── Test 6: Eliminación exitosa ───────────────────────────────────────
    [Fact]
    public async Task EliminarCliente_ExistentePreviamente_DebeRemoverlo()
    {
        // Arrange
        var (svc, _, _) = CrearServicio();
        var creado = await svc.CrearClienteAsync(JoseLemaDto());

        // Act
        await svc.EliminarClienteAsync(creado.ClienteId);

        // Assert
        var eliminado = await svc.ObtenerClientePorIdAsync(creado.ClienteId);
        eliminado.Should().BeNull();
    }

    // ── Test 7: Obtener todos ─────────────────────────────────────────────
    [Fact]
    public async Task ObtenerTodosLosClientes_ConVariosClientes_DebeRetornarLista()
    {
        // Arrange
        var (svc, _, _) = CrearServicio();
        await svc.CrearClienteAsync(JoseLemaDto());
        await svc.CrearClienteAsync(MarianelaDto());

        // Act
        var lista = await svc.ObtenerTodosLosClientesAsync();

        // Assert
        lista.Should().HaveCount(2);
        lista.Select(c => c.Nombre).Should().Contain(["Jose Lema", "Marianela Montalvo"]);
    }

    // ── Test 8: Eliminar inexistente lanza excepción ──────────────────────
    [Fact]
    public async Task EliminarCliente_Inexistente_DebeLanzarClienteNotFoundException()
    {
        // Arrange
        var (svc, _, _) = CrearServicio();

        // Act
        Func<Task> act = () => svc.EliminarClienteAsync(9999);

        // Assert
        await act.Should().ThrowAsync<ClienteNotFoundException>();
    }

    // ── Actualizar cliente inexistente lanza excepción ────────────────────
    [Fact]
    public async Task ActualizarCliente_Inexistente_DebeLanzarClienteNotFoundException()
    {
        // Arrange
        var (svc, _, _) = CrearServicio();
        var actualizarDto = new ActualizarClienteDto(
            "Nombre", "Masculino", 30, "Direccion", "0999999999", "1234", Estado: true);

        // Act
        Func<Task> act = () => svc.ActualizarClienteAsync(9999, actualizarDto);

        // Assert
        await act.Should().ThrowAsync<ClienteNotFoundException>();
    }

    // ── Sincronización entre microservicios: eliminación publica evento ───
    private static (IClienteService svc, FakeEventBus eventBus) CrearServicioConEventBus()
    {
        var repo = new FakeClienteRepository();
        var hasher = new FakePasswordHasher();
        var eventBus = new FakeEventBus();
        return (new RealClienteService(repo, hasher, new FakeUnitOfWork(), eventBus), eventBus);
    }

    [Fact]
    public async Task EliminarCliente_DespuesDeEliminar_DebePublicarClienteEliminadoEvent()
    {
        // Arrange
        var (svc, eventBus) = CrearServicioConEventBus();
        var creado = await svc.CrearClienteAsync(JoseLemaDto());
        eventBus.EventosPublicados.Clear();

        // Act
        await svc.EliminarClienteAsync(creado.ClienteId);

        // Assert
        eventBus.EventosPublicados.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new ClienteEliminadoEvent(creado.ClienteId));
    }
}
