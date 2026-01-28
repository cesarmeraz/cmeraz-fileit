using FileIt.Domain.Entities;

namespace FileIt.Domain.Interfaces;

public interface IRepository<T>
    where T : class, IAuditable
{
    // Basic CRUD operations
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> AddAsync(T entity);
    Task<T?> UpdateAsync(T entity, IDictionary<string, EntityOptions>? dataDictionary = null);
}
