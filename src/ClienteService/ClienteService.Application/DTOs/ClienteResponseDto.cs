namespace ClienteService.Application.DTOs;

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
