namespace ClienteService.Application.Events;

public record ClienteCreadoEvent(
    long ClienteId,
    string Nombre,
    string Identificacion,
    string Direccion,
    string Telefono,
    bool Estado
);
