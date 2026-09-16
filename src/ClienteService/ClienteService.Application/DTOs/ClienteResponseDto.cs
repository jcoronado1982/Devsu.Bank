namespace ClienteService.Application.DTOs;

/// <summary>
/// Contrato de salida público de /clientes (GET, POST, PUT, PATCH). Omite intencionalmente
/// Contrasena para cumplir la regla de seguridad OWASP del proyecto: ningún endpoint GET debe
/// exponer la contraseña, ni siquiera en su forma hasheada. ClientesController es responsable
/// de mapear siempre ClienteDto -> ClienteResponseDto antes de serializar la respuesta.
/// </summary>
public record ClienteResponseDto(
    long ClienteId,
    string Nombre,
    string Genero,
    int Edad,
    string Identificacion,
    string Direccion,
    string Telefono,
    bool Estado
);
