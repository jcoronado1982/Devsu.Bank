using System.Net;
using System.Text.Json;
using ClienteService.Application.Exceptions;

namespace ClienteService.Api.Middlewares;

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
                statusCode = HttpStatusCode.Conflict;
                mensaje = idEx.Message;
                _logger.LogWarning("Conflicto de identificación duplicada: {Mensaje}", idEx.Message);
                break;

            case ClienteNotFoundException nfEx:
                statusCode = HttpStatusCode.NotFound;
                mensaje = nfEx.Message;
                _logger.LogWarning("Cliente no encontrado: {Mensaje}", nfEx.Message);
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
