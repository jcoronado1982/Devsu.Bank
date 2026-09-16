using System.Text.Json;
using ClienteService.Application.Exceptions;
using ClienteService.Application.Ports;
using Microsoft.Extensions.Logging;

namespace ClienteService.Infrastructure.Adapters;

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

            return documento.RootElement.ValueKind == JsonValueKind.Array
                   && documento.RootElement.GetArrayLength() > 0;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "No se pudo verificar cuentas asociadas para el cliente {ClienteId} en CuentaMovimientoService", clienteId);
            throw new ServicioCuentasNoDisponibleException();
        }
    }
}
