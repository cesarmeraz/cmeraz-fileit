using System.Reflection;
using FileIt.Domain.Entities;
using FileIt.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FileIt.Infrastructure.Data
{
    public abstract class BaseRepository<T> : IRepository<T>
        where T : class, IAuditable
    {
        protected IDbContextFactory<CommonDbContext> Factory { get; set; }

        public BaseRepository(IDbContextFactory<CommonDbContext> dbContextFactory)
        {
            this.Factory = dbContextFactory;
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            using var dbContext = Factory.CreateDbContext();
            return await dbContext.Set<T>().FindAsync(id);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            using var dbContext = Factory.CreateDbContext();
            return await dbContext.Set<T>().ToListAsync();
        }

        public async Task<T?> AddAsync(T entity)
        {
            entity.CreatedOn = DateTime.Now;
            entity.ModifiedOn = DateTime.Now;
            using var dbContext = Factory.CreateDbContext();
            await dbContext.Set<T>().AddAsync(entity);
            await dbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<T?> UpdateAsync(
            T entity,
            IDictionary<string, EntityOptions>? dataDictionary = null
        )
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            T? existingEntity = null;
            using (var dbContext = Factory.CreateDbContext())
            {
                existingEntity = dbContext.Set<T>().Find(entity.Id);
            }
            if (existingEntity == null)
                throw new ArgumentNullException(nameof(existingEntity));

            if (dataDictionary != null)
            {
                var type = entity?.GetType() ?? typeof(T);
                var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.GetIndexParameters().Length == 0)
                    .ToDictionary(x => x.Name);
                foreach (var optionSet in dataDictionary)
                {
                    var prop = props[optionSet.Key];
                    if (prop == null)
                        continue;
                    if (prop.GetValue(entity) != null)
                        continue;
                    var entryPair = dataDictionary[optionSet.Key];
                    if (entryPair.DefaultValue == null && !entryPair.Force)
                        continue;
                    props[optionSet.Key].SetValue(entity, entryPair.DefaultValue);
                }
            }

            using var dbContextUpdate = Factory.CreateDbContext();
            dbContextUpdate.Entry(existingEntity).CurrentValues.SetValues(entity!);
            foreach (var property in dbContextUpdate.Entry(existingEntity!).Properties)
            {
                bool allowReiteration = false;
                if (dataDictionary != null && dataDictionary.ContainsKey(property.Metadata.Name))
                {
                    var options = dataDictionary[property.Metadata.Name];
                    allowReiteration = options.AllowReiteration;
                }
                if (property.CurrentValue == null || !property.IsModified)
                {
                    dbContextUpdate
                        .Entry(existingEntity)
                        .Property(property.Metadata.Name)
                        .IsModified = allowReiteration;
                }
            }
            existingEntity.ModifiedOn = DateTime.Now;
            await dbContextUpdate.SaveChangesAsync();
            return existingEntity;
        }
    }
}
