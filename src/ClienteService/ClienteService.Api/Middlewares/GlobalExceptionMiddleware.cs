using System.Net;
using System.Text.Json;
using ClienteService.Application.Exceptions;

namespace ClienteService.Api.Middlewares;

/// <summary>
/// Middleware de manejo centralizado de excepciones (envuelve toda la pipeline HTTP). Traduce
/// las excepciones de negocio de ClienteService.Application a códigos HTTP específicos y, para
/// cualquier excepción no reconocida, responde 500 sin filtrar detalles internos (stack trace,
/// mensajes de infraestructura) al cliente HTTP, registrando el detalle completo solo en logs.
/// </summary>
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
            case IdentificacionDuplicadaException idEx:
                // EB-06: identificación duplicada -> 409 Conflict.
                statusCode = HttpStatusCode.Conflict;
                mensaje = idEx.Message;
                _logger.LogWarning("Conflicto de identificación duplicada: {Mensaje}", idEx.Message);
                break;

            case ClienteNotFoundException nfEx:
                // EB-07: cliente inexistente en actualizar/eliminar -> 404, mensaje exacto
                // "Cliente no encontrado" definido en la propia excepción.
                statusCode = HttpStatusCode.NotFound;
                mensaje = nfEx.Message;
                _logger.LogWarning("Cliente no encontrado: {Mensaje}", nfEx.Message);
                break;

            case ClienteConCuentasAsociadasException cuentasEx:
                // Eliminar un cliente con cuentas activas se trata como conflicto de estado
                // del recurso (409), no como solicitud inválida (400).
                statusCode = HttpStatusCode.Conflict;
                mensaje = cuentasEx.Message;
                _logger.LogWarning("Eliminación rechazada por cuentas asociadas: {Mensaje}", cuentasEx.Message);
                break;

            case ServicioCuentasNoDisponibleException noDispEx:
                // La verificación de cuentas asociadas depende de CuentaMovimientoService; si
                // no responde, se comunica como 503 (falla de dependencia externa) y no como
                // error genérico del propio ClienteService.
                statusCode = HttpStatusCode.ServiceUnavailable;
                mensaje = noDispEx.Message;
                _logger.LogError("No se pudo verificar cuentas asociadas (CuentaMovimientoService no disponible)");
                break;

            case ArgumentOutOfRangeException outEx:
                // Errores de validación de dominio (Persona/Cliente) lanzados como
                // Argument(OutOfRange)Exception se mapean a 400 Bad Request.
                statusCode = HttpStatusCode.BadRequest;
                mensaje = outEx.Message;
                _logger.LogWarning("Argumento fuera de rango: {Mensaje}", outEx.Message);
                break;

            case ArgumentException argEx:
                statusCode = HttpStatusCode.BadRequest;
                mensaje = argEx.Message;
                _logger.LogWarning("Argumento inválido: {Mensaje}", argEx.Message);
                break;

            default:
                // Cualquier excepción no prevista se trata como 500 y se registra completa
                // (con stack trace) en logs, pero el mensaje devuelto al cliente se mantiene
                // genérico ("Error interno del servidor") para no filtrar detalles internos.
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
