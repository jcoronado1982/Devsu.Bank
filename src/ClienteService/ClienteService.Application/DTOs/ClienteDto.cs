namespace ClienteService.Application.DTOs;
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
