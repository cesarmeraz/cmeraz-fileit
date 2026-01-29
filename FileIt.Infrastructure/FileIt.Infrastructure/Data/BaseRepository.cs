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

        /// <summary>
        /// In this implementation, we pass a detached entity
        /// and use property comparison to update an attached
        /// one, then save the changes. This is a naive developer
        /// concept as a default when requirements are not
        /// specified, approved and deployed.
        /// Naive, as in not understanding the specifics of the
        /// client, we provide an intuited interpretation. As
        /// developer, each of us are custodians to the data
        /// provided, but not knowing how the data relates to itself.
        /// And as default, we thus offer the interpretations
        /// that preserve the data provided but cannot assume
        /// action for null values. This is because in the
        /// moment we dont know with certainty if this was an
        /// intentional voidance of data, or a loss due to
        /// transit, or a mistaken omission, or an attempt to hide.
        /// Neither can we assume what the subsequent actions may be, triggered
        /// by value reiteratiom or repetition. It may be that
        /// this execution is reloading a source batch for correcting a problemn, or we
        /// are a problem that is causing the reiteration. Those are withheld.
        /// For any properties that should have a different default
        /// value, pass a data definition keyed on property name.
        /// For any property that should bear reiteration, include
        /// that in the data definition.
        /// Any property name in the dataDictionary, but not in the entity
        /// will be ignored. Otherwise a property name
        /// Any defaultOverride a blank or given value
        /// will be transferred to that property name.
        /// It is highly recommended to override this method
        /// with specific business logic.
        /// For code examples, see the unit tests.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<T?> UpdateAsync(
            T entity,
            IDictionary<string, EntityOptions>? dataDictionary = null
        )
        {
            if (entity == null) //Latest state of the Update Data
                throw new ArgumentNullException(nameof(entity));

            T? existingEntity = null;
            using (var dbContext = Factory.CreateDbContext())
            {
                existingEntity = dbContext.Set<T>().Find(entity.Id);
            }
            if (existingEntity == null) //Old state of the Updated Data
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
                    {
                        // if the property name doesn't exist, skip
                        continue;
                    }
                    if (prop.GetValue(entity) != null)
                    {
                        // if the property isn't null, skip
                        continue;
                    }
                    var entryPair = dataDictionary[optionSet.Key];
                    if (entryPair.DefaultValue == null && !entryPair.Force)
                    {
                        continue;
                    }
                    props[optionSet.Key].SetValue(entity, entryPair.DefaultValue);
                }
            }

            using var dbContextUpdate = Factory.CreateDbContext();
            //We will set all properties from entity to existingEntity
            dbContextUpdate.Entry(existingEntity).CurrentValues.SetValues(entity!);
            //We will check all propery's current value.
            foreach (var property in dbContextUpdate.Entry(existingEntity!).Properties)
            {
                bool allowReiteration = false;
                if (dataDictionary != null && dataDictionary[property.Metadata.Name] != null)
                {
                    if (dataDictionary[property.Metadata.Name] != null)
                    {
                        var options = dataDictionary[property.Metadata.Name];
                        allowReiteration = options.AllowReiteration;
                    }
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
