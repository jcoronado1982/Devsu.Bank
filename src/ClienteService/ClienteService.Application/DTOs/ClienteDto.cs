namespace ClienteService.Application.DTOs;

/// <summary>
/// DTO interno de transporte entre ClienteService (Application) y sus consumidores dentro
/// del mismo proceso (controladores). Incluye Contrasena (el hash BCrypt) porque es un modelo
/// interno, NO el contrato expuesto por la API: el controlador SIEMPRE lo convierte a
/// ClienteResponseDto antes de responder un GET, que omite este campo por completo para que
/// el hash jamás salga de la aplicación.
/// </summary>
public record ClienteDto(
    long ClienteId,
    string Nombre,
    string Genero,
    int Edad,
    string Identificacion,
    string Direccion,
    string Telefono,
    string Contrasena,
    bool Estado
);
