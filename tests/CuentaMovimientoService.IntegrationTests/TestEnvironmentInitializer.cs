using System.Runtime.CompilerServices;

namespace CuentaMovimientoService.IntegrationTests;

internal static class TestEnvironmentInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Deshabilita Ryuk para compatibilidad total con Docker-in-Docker y entornos restringidos
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
    }
}
