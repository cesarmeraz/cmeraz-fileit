
public interface IRepository<T>
    where T : class
{
    // Basic CRUD operations
    T GetById(object id);
    IEnumerable<T> GetAll();
    void Add(T entity);
    void Update(T entity);
    void Delete(T entity);
}
