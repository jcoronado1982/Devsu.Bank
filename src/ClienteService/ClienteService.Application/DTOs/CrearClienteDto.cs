namespace ClienteService.Application.DTOs;

/// <summary>
/// DTO de entrada para POST /clientes. Contrasena viaja aquí en texto plano desde el cliente
/// HTTP; ClienteService la hashea con BCrypt antes de construir la entidad Cliente, por lo que
/// el texto plano nunca llega a persistirse. Estado por defecto es true (cliente activo al crearse).
/// No incluye saldoActual ni ningún campo calculado: ese tipo de dato pertenece a CuentaMovimientoService.
/// </summary>
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
