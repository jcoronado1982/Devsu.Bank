namespace ClienteService.Application.DTOs;

public record ActualizarClienteDto(
    string Nombre,
    string Genero,
    int Edad,
    string Direccion,
    string Telefono,
    string Contrasena,
    bool Estado
);
