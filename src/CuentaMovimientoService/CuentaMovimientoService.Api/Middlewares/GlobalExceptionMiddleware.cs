using System.Net;
using System.Text.Json;
using CuentaMovimientoService.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CuentaMovimientoService.Api.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        string mensaje = "Error interno del servidor";

        switch (exception)
        {
            case SaldoNoDisponibleException saldoEx:
                statusCode = HttpStatusCode.BadRequest;
                mensaje = "Saldo no disponible";
                _logger.LogWarning("Transacción rechazada por saldo no disponible (EB-01)");
                break;

            case CupoDiarioExcedidoException cupoEx:
                statusCode = HttpStatusCode.BadRequest;
                mensaje = "Cupo diario Excedido";
                _logger.LogWarning("Transacción rechazada por cupo diario excedido (EB-03)");
                break;

            case CuentaInactivaException inactEx:
                statusCode = HttpStatusCode.BadRequest;
                mensaje = "Cuenta inactiva";
                _logger.LogWarning("Transacción rechazada por cuenta inactiva (EB-04)");
                break;

            case ClienteNoEncontradoException cliEx:
                statusCode = HttpStatusCode.NotFound;
                mensaje = "Cliente no encontrado";
                _logger.LogWarning("Cliente no encontrado (EB-07)");
                break;

            case CuentaNotFoundException ctaEx:
                statusCode = HttpStatusCode.NotFound;
                mensaje = "Cuenta no encontrada";
                _logger.LogWarning("Cuenta no encontrada: {Mensaje}", ctaEx.Message);
                break;

            case NumeroCuentaDuplicadoException numCtaEx:
                statusCode = HttpStatusCode.Conflict;
                mensaje = numCtaEx.Message;
                _logger.LogWarning("Conflicto de número de cuenta duplicado: {Mensaje}", numCtaEx.Message);
                break;

            case ArgumentOutOfRangeException outEx:
                statusCode = HttpStatusCode.BadRequest;
                mensaje = outEx.Message;
                _logger.LogWarning("Argumento fuera de rango: {Mensaje}", outEx.Message);
                break;

            case ArgumentException argEx:
                statusCode = HttpStatusCode.BadRequest;
                mensaje = argEx.Message;
                _logger.LogWarning("Argumento inválido: {Mensaje}", argEx.Message);
                break;

            case DbUpdateConcurrencyException:
                statusCode = HttpStatusCode.Conflict;
                mensaje = "La cuenta fue modificada por otra operación, intente nuevamente";
                _logger.LogWarning("Conflicto de concurrencia optimista detectado (EB-08)");
                break;

            default:
                _logger.LogError(exception, "Error no controlado procesando la solicitud HTTP");
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var respuesta = new
        {
            timestamp = DateTime.UtcNow.ToString("o"),
            codigo = (int)statusCode,
            mensaje
        };

        var opcionesJson = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(respuesta, opcionesJson));
    }
}
