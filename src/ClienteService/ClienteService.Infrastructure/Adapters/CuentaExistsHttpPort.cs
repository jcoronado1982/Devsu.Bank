using System.Text.Json;
using ClienteService.Application.Exceptions;
using ClienteService.Application.Ports;
using Microsoft.Extensions.Logging;

namespace ClienteService.Infrastructure.Adapters;

/// <summary>
/// Adaptador HTTP de ICuentaExistsPort: implementa la única llamada síncrona entre
/// microservicios permitida en este proyecto (GET /cuentas?clienteId= en CuentaMovimientoService),
/// usada exclusivamente para bloquear DELETE /clientes/{id} cuando el cliente aún tiene cuentas.
/// El HttpClient se registra en Program.cs vía AddHttpClient con un timeout de 5 segundos.
/// </summary>
public class CuentaExistsHttpPort : ICuentaExistsPort
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CuentaExistsHttpPort> _logger;

    public CuentaExistsHttpPort(HttpClient httpClient, ILogger<CuentaExistsHttpPort> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> ClienteTieneCuentasAsync(long clienteId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/cuentas?clienteId={clienteId}");
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var documento = await JsonDocument.ParseAsync(stream);

            // El endpoint /cuentas de CuentaMovimientoService responde un array JSON; basta con
            // que tenga al menos un elemento para considerar que el cliente tiene cuentas activas.
            return documento.RootElement.ValueKind == JsonValueKind.Array
                   && documento.RootElement.GetArrayLength() > 0;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // Fail-closed: si no se puede confirmar la ausencia de cuentas (servicio caído,
            // timeout o respuesta corrupta), se rechaza la eliminación en vez de asumir que no
            // tiene cuentas, para no arriesgar dejar cuentas huérfanas sin cliente dueño.
            _logger.LogError(ex, "No se pudo verificar cuentas asociadas para el cliente {ClienteId} en CuentaMovimientoService", clienteId);
            throw new ServicioCuentasNoDisponibleException();
        }
    }
}
