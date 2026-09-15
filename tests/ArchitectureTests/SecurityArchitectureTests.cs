using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Devsu.ArchitectureTests;

/// <summary>
/// Pruebas de Arquitectura de Seguridad (OWASP & 12-Factor App & SEGURIDAD_Y_PROTECCION_DATOS.md).
/// Estas pruebas impiden mecánicamente la introducción de credenciales quemadas
/// y garantizan la sanitización estricta de DTOs en el pipeline de CI/CD.
/// </summary>
public class SecurityArchitectureTests
{
    private static readonly Assembly ClienteAppAssembly = typeof(ClienteService.Application.DTOs.ClienteDto).Assembly;

    [Fact]
    public void Pilar1_ClienteResponseDto_NoDebeExponerContrasenaNiPassword()
    {
        // Regla Inmutable: Las contraseñas (ni en plano ni hasheadas) JAMÁS deben salir en DTOs de respuesta.
        var responseType = ClienteAppAssembly.GetType("ClienteService.Application.DTOs.ClienteResponseDto");
        responseType.Should().NotBeNull("ClienteResponseDto debe existir en la capa Application");

        var propertyNames = responseType!.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name.ToLowerInvariant())
            .ToList();

        propertyNames.Should().NotContain("contrasena", "ClienteResponseDto no debe exponer la propiedad contrasena");
        propertyNames.Should().NotContain("contraseña", "ClienteResponseDto no debe exponer la propiedad contraseña");
        propertyNames.Should().NotContain("password", "ClienteResponseDto no debe exponer la propiedad password");
        propertyNames.Should().NotContain("passwordhash", "ClienteResponseDto no debe exponer ningún hash de contraseña");
    }

    [Fact]
    public void Pilar5_AppSettings_NoDebenContenerContrasenasEnTextoPlano()
    {
        // Regla Inmutable: Prohibido quemar cadenas de conexión o contraseñas en archivos rastreados por Git.
        var solutionRoot = GetSolutionRoot();
        var appSettingsFiles = Directory.GetFiles(solutionRoot, "appsettings*.json", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/bin/") && !f.Contains("/obj/") && !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
            .ToList();

        appSettingsFiles.Should().NotBeEmpty("deben encontrarse los archivos de configuración appsettings");

        var passwordRegex = new Regex(@"(?i)Password\s*=\s*(?<pwd>[^;""\s]+)", RegexOptions.Compiled);

        foreach (var file in appSettingsFiles)
        {
            var content = File.ReadAllText(file);
            var match = passwordRegex.Match(content);

            match.Success.Should().BeFalse(
                $"El archivo '{Path.GetRelativePath(solutionRoot, file)}' contiene una contraseña quemada: '{match.Value}'. " +
                "Las credenciales deben inyectarse exclusivamente por variables de entorno en tiempo de ejecución (Pilar 5).");
        }
    }

    [Fact]
    public void Pilar5_CodigoFuente_NoDebeContenerCadenasDeConexionConPasswordQuemadas()
    {
        // Regla Inmutable: Ningún archivo C# dentro de src/ debe contener Password= quemado en duro.
        var solutionRoot = GetSolutionRoot();
        var srcDir = Path.Combine(solutionRoot, "src");

        var csFiles = Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/bin/") && !f.Contains("/obj/") && !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
            .ToList();

        csFiles.Should().NotBeEmpty("deben encontrarse los archivos de código fuente en src/");

        var passwordRegex = new Regex(@"(?i)Password\s*=\s*[^;""\s]+", RegexOptions.Compiled);

        foreach (var file in csFiles)
        {
            var content = File.ReadAllText(file);
            var match = passwordRegex.Match(content);

            match.Success.Should().BeFalse(
                $"El archivo de código '{Path.GetRelativePath(solutionRoot, file)}' contiene una credencial quemada: '{match.Value}'. " +
                "El código C# debe adherirse a Fail-Secure e inyectar credenciales vía IConfiguration / Environment (Pilar 5).");
        }
    }

    [Fact]
    public void Pilar5_EnvExample_DebeExistirEnLaRaiz_Y_NoEstarIgnorado()
    {
        // Regla: Debe proveerse la plantilla .env.example para que los evaluadores puedan ejecutar la solución.
        var solutionRoot = GetSolutionRoot();
        var envExamplePath = Path.Combine(solutionRoot, ".env.example");

        File.Exists(envExamplePath).Should().BeTrue("El archivo .env.example debe existir en la raíz del repositorio.");

        var content = File.ReadAllText(envExamplePath);
        content.Should().Contain("POSTGRES_PASSWORD", ".env.example debe documentar POSTGRES_PASSWORD");
        content.Should().Contain("RABBITMQ_DEFAULT_PASS", ".env.example debe documentar RABBITMQ_DEFAULT_PASS");
    }

    [Fact]
    public void Pilar5_DbContextFactories_NoContienenContrasenasEnConexionPorDefecto()
    {
        var clienteFactory = new ClienteService.Infrastructure.Persistence.ClienteDbContextFactory();
        using var clienteCtx = clienteFactory.CreateDbContext([]);
        var clienteConn = clienteCtx.Database.GetConnectionString();
        clienteConn.Should().NotContain("Password=Privado", "ClienteDbContextFactory no debe quemar contraseñas");

        var cuentaFactory = new CuentaMovimientoService.Infrastructure.Persistence.CuentaMovimientoDbContextFactory();
        using var cuentaCtx = cuentaFactory.CreateDbContext([]);
        var cuentaConn = cuentaCtx.Database.GetConnectionString();
        cuentaConn.Should().NotContain("Password=Privado", "CuentaMovimientoDbContextFactory no debe quemar contraseñas");
    }

    private static string GetSolutionRoot()
    {
        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        var directory = new DirectoryInfo(currentDir);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "docker-compose.yml")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("No se pudo localizar la raíz de la solución.");
    }
}
