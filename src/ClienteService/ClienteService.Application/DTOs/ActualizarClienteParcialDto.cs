namespace ClienteService.Application.DTOs;

public record ActualizarClienteParcialDto(
    string? Nombre = null,
    string? Genero = null,
    int? Edad = null,
    string? Direccion = null,
    string? Telefono = null,
    string? Contrasena = null,
    bool? Estado = null
);
