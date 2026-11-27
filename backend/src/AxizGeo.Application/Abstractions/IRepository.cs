namespace AxizGeo.Application.Abstractions;

/// <summary>
/// Patrón Repository genérico para desacoplar la lógica de negocio del ORM (EF Core).
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}

/// <summary>
/// Patrón Unit of Work: garantiza atomicidad transaccional al persistir múltiples cambios.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
