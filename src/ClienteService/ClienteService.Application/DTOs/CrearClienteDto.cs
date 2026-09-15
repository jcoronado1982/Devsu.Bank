namespace ClienteService.Application.DTOs;
public record CrearClienteDto(
    string Nombre,
    string Genero,
    int Edad,
    string Identificacion,
    string Direccion,
    string Telefono,
    string Contrasena,
    bool Estado = true
);
