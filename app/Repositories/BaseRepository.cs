using FileIt.App.Data;
using FileIt.App.Models;

namespace FileIt.App.Repositories
{
    public class BaseRepository<T> : IRepository<T>
        where T : class
    {
        protected readonly AppDbContext dbContext;
        protected readonly AppConfig appConfig;

        public BaseRepository(AppDbContext dbContext, AppConfig appConfig)
        {
            this.dbContext = dbContext;
            this.appConfig = appConfig;
        }

        public T GetById(object id)
        {
            return dbContext.Set<T>().Find(id);
        }

        public IEnumerable<T> GetAll()
        {
            return dbContext.Set<T>().ToList();
        }

        public void Add(T entity)
        {
            dbContext.Set<T>().Add(entity);
            dbContext.SaveChanges();
        }

        public void Update(T entity)
        {
            dbContext.Set<T>().Update(entity);
            dbContext.SaveChanges();
        }

        public void Delete(T entity)
        {
            dbContext.Set<T>().Remove(entity);
            dbContext.SaveChanges();
        }
    }
}
