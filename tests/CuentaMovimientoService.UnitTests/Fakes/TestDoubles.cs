using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CuentaMovimientoService.Application.Ports;
using CuentaMovimientoService.Domain.Entities;

namespace CuentaMovimientoService.UnitTests.Application;

public class FakeCuentaRepository : ICuentaRepository
{
    public readonly List<Cuenta> Store = new();
    public Task<Cuenta?> ObtenerPorNumeroCuentaAsync(string n) => Task.FromResult(Store.FirstOrDefault(c => c.NumeroCuenta == n));
    public Task<IEnumerable<Cuenta>> ObtenerPorClienteIdAsync(long id) => Task.FromResult<IEnumerable<Cuenta>>(Store.Where(c => c.ClienteId == id).ToList());
    public Task<IEnumerable<Cuenta>> ObtenerTodasAsync() => Task.FromResult<IEnumerable<Cuenta>>(Store.ToList());
    public Task AddAsync(Cuenta c) { Store.Add(c); return Task.CompletedTask; }
    public void Remove(Cuenta c) => Store.Remove(c);
    public Task SaveChangesAsync() => Task.CompletedTask;
}

public class FakeClienteExistsPort : IClienteExistsPort
{
    private readonly HashSet<long> _existentes;
    public FakeClienteExistsPort(params long[] ids) => _existentes = new HashSet<long>(ids);
    public Task<bool> ExisteClienteAsync(long id) => Task.FromResult(_existentes.Contains(id));
}

public class FakeUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
}
