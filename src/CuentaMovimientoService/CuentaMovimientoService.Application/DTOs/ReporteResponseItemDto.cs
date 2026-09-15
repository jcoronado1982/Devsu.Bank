using System.Text.Json.Serialization;

namespace CuentaMovimientoService.Application.DTOs;

public class ReporteResponseItemDto
{
    [JsonPropertyName("fecha")]
    public string Fecha { get; set; } = string.Empty;

    [JsonPropertyName("cliente")]
    public string Cliente { get; set; } = string.Empty;

    [JsonPropertyName("numeroCuenta")]
    public string NumeroCuenta { get; set; } = string.Empty;

    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = string.Empty;

    [JsonPropertyName("saldoInicial")]
    public decimal SaldoInicial { get; set; }

    [JsonPropertyName("estado")]
    public bool Estado { get; set; }

    [JsonPropertyName("movimiento")]
    public decimal Movimiento { get; set; }

    [JsonPropertyName("saldoDisponible")]
    public decimal SaldoDisponible { get; set; }

    public static ReporteResponseItemDto DesdeReporteMovimiento(ReporteMovimientoDto i) => new()
    {
        Fecha = i.Fecha,
        Cliente = i.Cliente,
        NumeroCuenta = i.NumeroCuenta,
        Tipo = i.Tipo,
        SaldoInicial = i.SaldoInicial,
        Estado = i.Estado,
        Movimiento = i.Movimiento,
        SaldoDisponible = i.SaldoDisponible
    };
}
