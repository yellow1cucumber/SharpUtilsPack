using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using Microsoft.EntityFrameworkCore;
using SharpUtils.Results;

namespace SharpUtils.Repository.Generic
{
    /// <summary>
    /// Entity Framework implementation of the generic repository pattern.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity that this repository manages.</typeparam>
    /// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
    public class EfGenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
        where TEntity : class
        where TKey : IEquatable<TKey>
    {
        protected readonly DbContext Context;
        protected readonly DbSet<TEntity> DbSet;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="EfGenericRepository{TEntity, TKey}"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public EfGenericRepository(DbContext context)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            this.DbSet = context.Set<TEntity>();
        }

        /// <summary>
        /// Occurs when an entity is changed.
        /// </summary>
        public event EventHandler<EntityChangedEventArgs<TEntity>>? EntityChanged;

        /// <summary>
        /// Gets the queryable collection of entities.
        /// </summary>
        public IQueryable<TEntity> Query => this.DbSet;

        #region SYNC
        #region Create Operations

        public Result<TEntity> Add(TEntity entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                var entry = this.DbSet.Add(entity);
                this.OnEntityChanged(entity, EntityChangeType.Added);
                return Result<TEntity>.Success(entry.Entity);
            }
            catch (Exception ex)
            {
                return Result<TEntity>.Failure($"Failed to add entity: {ex.Message}");
            }
        }

        public Result<IEnumerable<TEntity>> AddRange(IEnumerable<TEntity> entities)
        {
            try
            {
                if (entities == null)
                    throw new ArgumentNullException(nameof(entities));

                var entityList = entities.ToList();
                this.DbSet.AddRange(entityList);

                foreach (var entity in entityList)
                {
                    this.OnEntityChanged(entity, EntityChangeType.Added);
                }

                return Result<IEnumerable<TEntity>>.Success(entityList);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TEntity>>.Failure($"Failed to add entities: {ex.Message}");
            }
        }

        #endregion

        #region Read Operations

        public Result<TEntity?> GetById(TKey id)
        {
            try
            {
                if (id == null)
                    throw new ArgumentNullException(nameof(id));

                var entity = this.DbSet.Find(id);
                return Result<TEntity?>.Success(entity);
            }
            catch (Exception ex)
            {
                return Result<TEntity?>.Failure($"Failed to get entity by ID: {ex.Message}");
            }
        }

        public Result<TEntity?> GetFirstOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var entity = this.DbSet.FirstOrDefault(predicate);
                return Result<TEntity?>.Success(entity);
            }
            catch (Exception ex)
            {
                return Result<TEntity?>.Failure($"Failed to get first entity: {ex.Message}");
            }
        }

        public Result<IEnumerable<TEntity>> GetAll()
        {
            try
            {
                var entities = this.DbSet.ToList();
                return Result<IEnumerable<TEntity>>.Success(entities);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TEntity>>.Failure($"Failed to get all entities: {ex.Message}");
            }
        }

        public Result<IEnumerable<TEntity>> GetWhere(Expression<Func<TEntity, bool>> predicate)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var entities = this.DbSet.Where(predicate).ToList();
                return Result<IEnumerable<TEntity>>.Success(entities);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TEntity>>.Failure($"Failed to get entities by predicate: {ex.Message}");
            }
        }

        public Result<IEnumerable<TEntity>> GetWithInclude(params Expression<Func<TEntity, object>>[] includes)
        {
            try
            {
                IQueryable<TEntity> query = this.DbSet;
                query = includes.Aggregate(query, (current, include) => current.Include(include));
                return Result<IEnumerable<TEntity>>.Success(query.ToList());
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TEntity>>.Failure($"Failed to get entities with includes: {ex.Message}");
            }
        }

        public Result<IEnumerable<TEntity>> GetWhereWithInclude(
            Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes)
        {
            try
            {
                IQueryable<TEntity> query = this.DbSet;
                query = includes.Aggregate(query, (current, include) => current.Include(include));
                return Result<IEnumerable<TEntity>>.Success(query.Where(predicate).ToList());
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TEntity>>.Failure($"Failed to get entities with predicate and includes: {ex.Message}");
            }
        }

        public PaginatedResult<TEntity> GetPaged(int pageNumber, int pageSize)
        {
            try
            {
                if (pageNumber <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than zero.");

                if (pageSize <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

                var totalItems = this.DbSet.Count();
                var items = this.DbSet.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                return totalItems == 0
                    ? PaginatedResult<TEntity>.Empty((uint)pageNumber, (uint)pageSize)
                    : PaginatedResult<TEntity>.Success(items, (uint)pageNumber, (uint)pageSize, (uint)totalItems);
            }
            catch (Exception ex)
            {
                return PaginatedResult<TEntity>.Failure($"Failed to get paged entities: {ex.Message}");
            }
        }

        public PaginatedResult<TEntity> GetPagedWhere(Expression<Func<TEntity, bool>> predicate, int pageNumber, int pageSize)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                if (pageNumber <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than zero.");

                if (pageSize <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

                var query = this.DbSet.Where(predicate);
                var totalItems = query.Count();
                var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                return totalItems == 0
                    ? PaginatedResult<TEntity>.Empty((uint)pageNumber, (uint)pageSize)
                    : PaginatedResult<TEntity>.Success(items, (uint)pageNumber, (uint)pageSize, (uint)totalItems);
            }
            catch (Exception ex)
            {
                return PaginatedResult<TEntity>.Failure($"Failed to get paged entities by predicate: {ex.Message}");
            }
        }

        public PaginatedResult<TEntity> GetPagedOrdered(
            Expression<Func<TEntity, TKey>> orderBy,
            int pageNumber,
            int pageSize,
            bool ascending = true)
        {
            try
            {
                if (orderBy == null)
                    throw new ArgumentNullException(nameof(orderBy));

                if (pageNumber <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than zero.");

                if (pageSize <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

                var query = ascending ? this.DbSet.OrderBy(orderBy) : this.DbSet.OrderByDescending(orderBy);
                var totalItems = query.Count();
                var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                return totalItems == 0
                    ? PaginatedResult<TEntity>.Empty((uint)pageNumber, (uint)pageSize)
                    : PaginatedResult<TEntity>.Success(items, (uint)pageNumber, (uint)pageSize, (uint)totalItems);
            }
            catch (Exception ex)
            {
                return PaginatedResult<TEntity>.Failure($"Failed to get paged ordered entities: {ex.Message}");
            }
        }

        public PaginatedResult<TEntity> GetPagedWhereWithInclude(
            Expression<Func<TEntity, bool>> predicate,
            int pageNumber,
            int pageSize,
            params Expression<Func<TEntity, object>>[] includes)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                if (pageNumber <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than zero.");

                if (pageSize <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

                IQueryable<TEntity> query = this.DbSet;

                query = query.Where(predicate);
                query = includes.Aggregate(query, (current, include) => current.Include(include));
                
                var totalItems = query.Count();
                var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                return totalItems == 0
                    ? PaginatedResult<TEntity>.Empty((uint)pageNumber, (uint)pageSize)
                    : PaginatedResult<TEntity>.Success(items, (uint)pageNumber, (uint)pageSize, (uint)totalItems);
            }
            catch (Exception ex)
            {
                return PaginatedResult<TEntity>.Failure($"Failed to get paged entities by predicate with includes: {ex.Message}");
            }
        }

        public PaginatedResult<TEntity> GetPagedOrderedWithInclude(
            Expression<Func<TEntity, TKey>> orderBy,
            int pageNumber,
            int pageSize,
            bool ascending = true,
            params Expression<Func<TEntity, object>>[] includes)
        {
            try
            {
                if (orderBy == null)
                    throw new ArgumentNullException(nameof(orderBy));

                if (pageNumber <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than zero.");

                if (pageSize <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

                IQueryable<TEntity> query = this.DbSet;

                query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
                query = includes.Aggregate(query, (current, include) => current.Include(include));
                
                var totalItems = query.Count();
                var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                return totalItems == 0
                    ? PaginatedResult<TEntity>.Empty((uint)pageNumber, (uint)pageSize)
                    : PaginatedResult<TEntity>.Success(items, (uint)pageNumber, (uint)pageSize, (uint)totalItems);
            }
            catch (Exception ex)
            {
                return PaginatedResult<TEntity>.Failure($"Failed to get paged ordered entities with includes: {ex.Message}");
            }
        }

        public Result<int> Count()
        {
            try
            {
                var count = this.DbSet.Count();
                return Result<int>.Success(count);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to count entities: {ex.Message}");
            }
        }

        public Result<int> CountWhere(Expression<Func<TEntity, bool>> predicate)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var count = this.DbSet.Count(predicate);
                return Result<int>.Success(count);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to count entities by predicate: {ex.Message}");
            }
        }

        public Result<bool> Exists(Expression<Func<TEntity, bool>> predicate)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var exists = this.DbSet.Any(predicate);
                return Result<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to check existence of entities by predicate: {ex.Message}");
            }
        }

        public Result<bool> ExistsById(TKey id)
        {
            try
            {
                if (id == null)
                    throw new ArgumentNullException(nameof(id));

                var entity = this.DbSet.Find(id);
                return Result<bool>.Success(entity != null);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to check existence of entity by ID: {ex.Message}");
            }
        }

        #endregion

        #region Update Operations

        public Result<TEntity> Update(TEntity entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                var entry = this.Context.Entry(entity);
                if (entry.State == EntityState.Detached)
                {
                    this.DbSet.Attach(entity);
                }
                entry.State = EntityState.Modified;
                this.OnEntityChanged(entity, EntityChangeType.Modified);
                return Result<TEntity>.Success(entity);
            }
            catch (Exception ex)
            {
                return Result<TEntity>.Failure($"Failed to update entity: {ex.Message}");
            }
        }

        public Result<IEnumerable<TEntity>> UpdateRange(IEnumerable<TEntity> entities)
        {
            try
            {
                if (entities == null)
                    throw new ArgumentNullException(nameof(entities));

                var entityList = entities.ToList();
                foreach (var entity in entityList)
                {
                    var entry = this.Context.Entry(entity);
                    if (entry.State == EntityState.Detached)
                    {
                        this.DbSet.Attach(entity);
                    }
                    entry.State = EntityState.Modified;
                    this.OnEntityChanged(entity, EntityChangeType.Modified);
                }

                return Result<IEnumerable<TEntity>>.Success(entityList);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TEntity>>.Failure($"Failed to update entities: {ex.Message}");
            }
        }

        #endregion

        #region Delete Operations

        public Result Delete(TEntity entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                if (this.Context.Entry(entity).State == EntityState.Detached)
                {
                    this.DbSet.Attach(entity);
                }
                this.DbSet.Remove(entity);
                this.OnEntityChanged(entity, EntityChangeType.Deleted);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to delete entity: {ex.Message}");
            }
        }

        public Result DeleteRange(IEnumerable<TEntity> entities)
        {
            try
            {
                if (entities == null)
                    throw new ArgumentNullException(nameof(entities));

                var entityList = entities.ToList();
                this.DbSet.RemoveRange(entityList);

                foreach (var entity in entityList)
                {
                    this.OnEntityChanged(entity, EntityChangeType.Deleted);
                }

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to delete entities: {ex.Message}");
            }
        }

        public Result DeleteById(TKey id)
        {
            try
            {
                if (id == null)
                    throw new ArgumentNullException(nameof(id));

                var entity = this.DbSet.Find(id);
                if (entity == null)
                {
                    return Result.Failure("Entity not found.");
                }

                this.DbSet.Remove(entity);
                this.OnEntityChanged(entity, EntityChangeType.Deleted);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to delete entity by ID: {ex.Message}");
            }
        }

        public Result DeleteWhere(Expression<Func<TEntity, bool>> predicate)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var entities = this.DbSet.Where(predicate).ToList();
                this.DbSet.RemoveRange(entities);

                foreach (var entity in entities)
                {
                    this.OnEntityChanged(entity, EntityChangeType.Deleted);
                }
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to delete entities by predicate: {ex.Message}");
            }
        }

        #endregion

        #region Bulk Operations

        public Result<int> BulkUpdate(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TEntity>> updateExpression)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                if (updateExpression == null)
                    throw new ArgumentNullException(nameof(updateExpression));

                var entities = this.DbSet.Where(predicate);
                foreach (var entity in entities)
                {
                    var updatedEntity = updateExpression.Compile().Invoke(entity);
                    this.Context.Entry(entity).CurrentValues.SetValues(updatedEntity);
                    this.OnEntityChanged(entity, EntityChangeType.Modified);
                }
                return Result<int>.Success(entities.Count());
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to bulk update entities: {ex.Message}");
            }
        }

        public Result<int> BulkDelete(Expression<Func<TEntity, bool>> predicate)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var entities = this.DbSet.Where(predicate);
                this.DbSet.RemoveRange(entities);
                foreach (var entity in entities)
                {
                    this.OnEntityChanged(entity, EntityChangeType.Deleted);
                }
                return Result<int>.Success(entities.Count());
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to bulk delete entities: {ex.Message}");
            }
        }
        #endregion

        #region Persistence

        public Result<int> SaveChanges()
        {
            try
            {
                var result = this.Context.SaveChanges();
                return Result<int>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to save changes: {ex.Message}");
            }
        }
        #endregion
        #endregion

        #region ASYNC

        #region Create Operations

        public async Task<Result<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                var entry = await this.DbSet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
                this.OnEntityChanged(entity, EntityChangeType.Added);

                return Result<TEntity>.Success(entry.Entity);
            }
            catch (Exception ex)
            {
                return Result<TEntity>.Failure($"Failed to add entity: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<TEntity>>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                if (entities == null)
                    throw new ArgumentNullException(nameof(entities));

                var entityList = entities.ToList();
                await this.DbSet.AddRangeAsync(entityList, cancellationToken).ConfigureAwait(false);

                foreach (var entity in entityList)
                {
                    this.OnEntityChanged(entity, EntityChangeType.Added);
                }
                return Result<IEnumerable<TEntity>>.Success(entityList);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TEntity>>.Failure($"Failed to add entities: {ex.Message}");
            }
        }

        #endregion

        #region Read Operations

        public async Task<Result<TEntity?>> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id == null)
                    throw new ArgumentNullException(nameof(id));

                var entity = await this.DbSet.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
                return Result<TEntity?>.Success(entity);
            }
            catch (Exception ex)
            {
                return Result<TEntity?>.Failure($"Failed to get entity by ID: {ex.Message}");
            }
        }

        public async Task<Result<TEntity?>> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var entity = await this.DbSet.FirstOrDefaultAsync(predicate, cancellationToken).ConfigureAwait(false);
                return Result<TEntity?>.Success(entity);
            }
            catch (Exception ex)
            {
                return Result<TEntity?>.Failure($"Failed to get first entity: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<TEntity>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var entities = await this.DbSet.ToListAsync(cancellationToken).ConfigureAwait(false);
                return Result<IEnumerable<TEntity>>.Success(entities);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TEntity>>.Failure($"Failed to get all entities: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<TEntity>>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var entities = await this.DbSet.Where(predicate).ToListAsync(cancellationToken).ConfigureAwait(false);
                return Result<IEnumerable<TEntity>>.Success(entities);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TEntity>>.Failure($"Failed to get entities by predicate: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<TEntity>>> GetWithIncludeAsync(
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object>>[] includes)
        {
            try
            {
                IQueryable<TEntity> query = this.DbSet;

                query = includes.Aggregate(query, (current, include) => current.Include(include));

                var entities = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
                return Result<IEnumerable<TEntity>>.Success(entities);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TEntity>>.Failure($"Failed to get entities with includes: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<TEntity>>> GetWhereWithIncludeAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object>>[] includes)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                IQueryable<TEntity> query = this.DbSet;

                query = query.Where(predicate);
                query = includes.Aggregate(query, (current, include) => current.Include(include));

                var entities = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
                return Result<IEnumerable<TEntity>>.Success(entities);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TEntity>>.Failure($"Failed to get entities with predicate and includes: {ex.Message}");
            }
        }

        public async Task<PaginatedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                if (pageNumber <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than zero.");

                if (pageSize <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

                var totalItems = await this.DbSet.CountAsync(cancellationToken).ConfigureAwait(false);
                var items = await this.DbSet
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                return totalItems == 0
                    ? PaginatedResult<TEntity>.Empty((uint)pageNumber, (uint)pageSize)
                    : PaginatedResult<TEntity>.Success(items, (uint)pageNumber, (uint)pageSize, (uint)totalItems);
            }
            catch (Exception ex)
            {
                return PaginatedResult<TEntity>.Failure($"Failed to get paged entities: {ex.Message}");
            }
        }

        public async Task<PaginatedResult<TEntity>> GetPagedWhereAsync(
            Expression<Func<TEntity, bool>> predicate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                if (pageNumber <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than zero.");

                if (pageSize <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

                var query = this.DbSet.Where(predicate);
                var totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);
                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                return totalItems == 0
                    ? PaginatedResult<TEntity>.Empty((uint)pageNumber, (uint)pageSize)
                    : PaginatedResult<TEntity>.Success(items, (uint)pageNumber, (uint)pageSize, (uint)totalItems);
            }
            catch (Exception ex)
            {
                return PaginatedResult<TEntity>.Failure($"Failed to get paged entities by predicate: {ex.Message}");
            }
        }

        public async Task<PaginatedResult<TEntity>> GetPagedOrderedAsync(
            Expression<Func<TEntity, TKey>> orderBy,
            int pageNumber,
            int pageSize,
            bool ascending = true,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (orderBy == null)
                    throw new ArgumentNullException(nameof(orderBy));

                if (pageNumber <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than zero.");

                if (pageSize <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

                var query = ascending ? this.DbSet.OrderBy(orderBy) : this.DbSet.OrderByDescending(orderBy);
                var totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);
                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                return totalItems == 0
                    ? PaginatedResult<TEntity>.Empty((uint)pageNumber, (uint)pageSize)
                    : PaginatedResult<TEntity>.Success(items, (uint)pageNumber, (uint)pageSize, (uint)totalItems);
            }
            catch (Exception ex)
            {
                return PaginatedResult<TEntity>.Failure($"Failed to get paged ordered entities: {ex.Message}");
            }
        }

        public async Task<PaginatedResult<TEntity>> GetPagedWhereWithIncludeAsync(
            Expression<Func<TEntity, bool>> predicate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object>>[] includes)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                if (pageNumber <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than zero.");

                if (pageSize <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

                IQueryable<TEntity> query = this.DbSet;

                query = query.Where(predicate);
                query = includes.Aggregate(query, (current, include) => current.Include(include));

                var totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);
                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                return totalItems == 0
                    ? PaginatedResult<TEntity>.Empty((uint)pageNumber, (uint)pageSize)
                    : PaginatedResult<TEntity>.Success(items, (uint)pageNumber, (uint)pageSize, (uint)totalItems);
            }
            catch (Exception ex)
            {
                return PaginatedResult<TEntity>.Failure($"Failed to get paged entities by predicate with includes: {ex.Message}");
            }
        }

        public async Task<PaginatedResult<TEntity>> GetPagedOrderedWithIncludeAsync(
            Expression<Func<TEntity, TKey>> orderBy,
            int pageNumber,
            int pageSize,
            bool ascending = true,
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object>>[] includes)
        {
            try
            {
                if (orderBy == null)
                    throw new ArgumentNullException(nameof(orderBy));

                if (pageNumber <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than zero.");

                if (pageSize <= 0)
                    throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");

                IQueryable<TEntity> query = this.DbSet;

                query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
                query = includes.Aggregate(query, (current, include) => current.Include(include));

                var totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);
                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                return totalItems == 0
                    ? PaginatedResult<TEntity>.Empty((uint)pageNumber, (uint)pageSize)
                    : PaginatedResult<TEntity>.Success(items, (uint)pageNumber, (uint)pageSize, (uint)totalItems);
            }
            catch (Exception ex)
            {
                return PaginatedResult<TEntity>.Failure($"Failed to get paged ordered entities with includes: {ex.Message}");
            }
        }

        public async Task<Result<int>> CountAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var count = await this.DbSet.CountAsync(cancellationToken).ConfigureAwait(false);
                return Result<int>.Success(count);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to count entities: {ex.Message}");
            }
        }

        public async Task<Result<int>> CountWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var count = await this.DbSet.CountAsync(predicate, cancellationToken).ConfigureAwait(false);
                return Result<int>.Success(count);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to count entities by predicate: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var exists = await this.DbSet.AnyAsync(predicate, cancellationToken).ConfigureAwait(false);
                return Result<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to check existence of entities by predicate: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ExistsByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id == null)
                    throw new ArgumentNullException(nameof(id));

                var entity = await this.DbSet.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
                return Result<bool>.Success(entity != null);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to check existence of entity by ID: {ex.Message}");
            }
        }

        #endregion

        #region Update Operations

        public async Task<Result<TEntity>> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                var entry = this.Context.Entry(entity);
                if (entry.State == EntityState.Detached)
                {
                    this.DbSet.Attach(entity);
                }

                entry.State = EntityState.Modified;
                this.OnEntityChanged(entity, EntityChangeType.Modified);
                return Result<TEntity>.Success(entity);
            }
            catch (Exception ex)
            {
                return Result<TEntity>.Failure($"Failed to update entity: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<TEntity>>> UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                if (entities == null)
                    throw new ArgumentNullException(nameof(entities));

                var entityList = entities.ToList();
                foreach (var entity in entityList)
                {
                    var entry = this.Context.Entry(entity);
                    if (entry.State == EntityState.Detached)
                    {
                        this.DbSet.Attach(entity);
                    }
                    entry.State = EntityState.Modified;
                    this.OnEntityChanged(entity, EntityChangeType.Modified);
                }
                return Result<IEnumerable<TEntity>>.Success(entityList);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TEntity>>.Failure($"Failed to update entities: {ex.Message}");
            }
        }

        #endregion

        #region Delete Operations

        public async Task<Result> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                if (this.Context.Entry(entity).State == EntityState.Detached)
                {
                    this.DbSet.Attach(entity);
                }

                this.DbSet.Remove(entity);
                this.OnEntityChanged(entity, EntityChangeType.Deleted);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to delete entity: {ex.Message}");
            }
        }

        public async Task<Result> DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                if (entities == null)
                    throw new ArgumentNullException(nameof(entities));

                var entityList = entities.ToList();
                this.DbSet.RemoveRange(entityList);
                foreach (var entity in entityList)
                {
                    this.OnEntityChanged(entity, EntityChangeType.Deleted);
                }
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to delete entities: {ex.Message}");
            }
        }

        public async Task<Result> DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id == null)
                    throw new ArgumentNullException(nameof(id));

                var entity = await this.DbSet.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
                if (entity == null)
                {
                    return Result.Failure("Entity not found.");
                }
                this.DbSet.Remove(entity);
                this.OnEntityChanged(entity, EntityChangeType.Deleted);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to delete entity by ID: {ex.Message}");
            }
        }

        public async Task<Result> DeleteWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var entities = await this.DbSet.Where(predicate).ToListAsync(cancellationToken).ConfigureAwait(false);
                this.DbSet.RemoveRange(entities);
                foreach (var entity in entities)
                {
                    this.OnEntityChanged(entity, EntityChangeType.Deleted);
                }
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to delete entities by predicate: {ex.Message}");
            }
        }

        #endregion

        #region Bulk Operations

        public async Task<Result<int>> BulkUpdateAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TEntity>> updateExpression, CancellationToken cancellationToken = default)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                if (updateExpression == null)
                    throw new ArgumentNullException(nameof(updateExpression));

                var entities = this.DbSet.Where(predicate);
                foreach (var entity in entities)
                {
                    var updatedEntity = updateExpression.Compile().Invoke(entity);
                    this.Context.Entry(entity).CurrentValues.SetValues(updatedEntity);
                    this.OnEntityChanged(entity, EntityChangeType.Modified);
                }
                return Result<int>.Success(entities.Count());
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to bulk update entities: {ex.Message}");
            }
        }

        public async Task<Result<int>> BulkDeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var entities = this.DbSet.Where(predicate);
                this.DbSet.RemoveRange(entities);
                foreach (var entity in entities)
                {
                    this.OnEntityChanged(entity, EntityChangeType.Deleted);
                }
                return Result<int>.Success(entities.Count());
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to bulk delete entities: {ex.Message}");
            }
        }

        #endregion

        #region Persistance

        public async Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await this.Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return Result<int>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Failed to save changes: {ex.Message}");
            }
        }

        #endregion

        #region Transactions

        public async Task<Result<TResult>> TransactionAsync<TResult>(
            Func<IGenericRepository<TEntity, TKey>, 
            Task<Result<TResult>>> operation, 
            CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            using var transaction = await this.Context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var result = await operation(this).ConfigureAwait(false);
                if (result.IsFailure)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return Result<TResult>.Failure(result.ErrorMessage ?? "Transaction operation failed.");
                }
                await this.Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return Result<TResult>.Success(result.Value!);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return Result<TResult>.Failure($"Transaction failed: {ex.Message}");
            }
        }

        #endregion

        #region Projections and Streaming

        public async Task<Result<TProjection>> GetProjectionAsync<TProjection>(
            Expression<Func<TEntity, TProjection>> projection,
            Expression<Func<TEntity, bool>>? predicate = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                if (projection == null)
                    throw new ArgumentNullException(nameof(projection));

                var projectedValue = await this.DbSet
                    .Where(predicate)
                    .Select(projection)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                return Result<TProjection>.Success(projectedValue!);
            }
            catch (Exception ex)
            {
                return Result<TProjection>.Failure($"Failed to get projection: {ex.Message}");
            }
        }

        public IAsyncEnumerable<Result<TEntity>> GetAllAsStreamAsync(int batchSize = 1000, CancellationToken cancellationToken = default)
        {
            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");

            return StreamEntitiesAsync(batchSize, cancellationToken);

            async IAsyncEnumerable<Result<TEntity>> StreamEntitiesAsync(int batchSize, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                var query = this.DbSet.AsNoTracking().AsAsyncEnumerable();
                var batch = new List<TEntity>(batchSize);

                await foreach (var entity in query.WithCancellation(cancellationToken))
                {
                    batch.Add(entity);
                    if (batch.Count == batchSize)
                    {
                        foreach (var item in batch)
                            yield return Result<TEntity>.Success(item);
                        batch.Clear();
                    }
                }

                foreach (var item in batch)
                    yield return Result<TEntity>.Success(item);
            }
        }

        #endregion

        #region Specification

        public Result<IEnumerable<TEntity>> GetBySpecification(ISpecification<TEntity> specification)
        {
            try
            {
                if (specification == null)
                    throw new ArgumentNullException(nameof(specification));

                var query = this.DbSet.AsQueryable();
                if (specification.Criteria != null)
                {
                    query = query.Where(specification.Criteria);
                }
                if (specification.Includes != null)
                {
                    query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
                }

                var entities = query.ToList();
                return Result<IEnumerable<TEntity>>.Success(entities);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TEntity>>.Failure($"Failed to get entities by specification: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<TEntity>>> GetBySpecificationAsync(
            ISpecification<TEntity> specification, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (specification == null)
                    throw new ArgumentNullException(nameof(specification));

                var query = this.DbSet.AsQueryable();
                if (specification.Criteria != null)
                {
                    query = query.Where(specification.Criteria);
                }
                if (specification.Includes != null)
                {
                    query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
                }
                var entities = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
                return Result<IEnumerable<TEntity>>.Success(entities);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TEntity>>.Failure($"Failed to get entities by specification: {ex.Message}");
            }
        }

        #endregion

        #region Partial Updates

        public Result<TEntity> UpdatePartial<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> propertyExpression, TProperty value)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                if (propertyExpression == null)
                    throw new ArgumentNullException(nameof(propertyExpression));

                var entry = this.Context.Entry(entity);
                if (entry.State == EntityState.Detached)
                {
                    this.DbSet.Attach(entity);
                }

                var memberExpression = propertyExpression.Body as MemberExpression;
                if (memberExpression == null)
                    throw new ArgumentException("The property expression must be a member expression.", nameof(propertyExpression));

                var propertyName = memberExpression.Member.Name;
                entry.Property(propertyName).CurrentValue = value;
                entry.Property(propertyName).IsModified = true;

                this.OnEntityChanged(entity, EntityChangeType.Modified);

                return Result<TEntity>.Success(entity);
            }
            catch (Exception ex)
            {
                return Result<TEntity>.Failure($"Failed to partially update entity: {ex.Message}");
            }
        }

        #endregion

        #endregion

        #region Validation and Safety
        public Result RollbackChanges()
        {
            try
            {
                foreach (var entry in this.Context.ChangeTracker.Entries())
                {
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            entry.State = EntityState.Unchanged;
                            break;
                        case EntityState.Added:
                            entry.State = EntityState.Detached;
                            break;
                        case EntityState.Deleted:
                            entry.Reload();
                            break;
                    }
                }
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure($"Failed to rollback changes: {ex.Message}");
            }
        }
        #endregion

        #region Helper Methods

        protected virtual void OnEntityChanged(TEntity entity, EntityChangeType changeType)
        {
            this.EntityChanged?.Invoke(this, new EntityChangedEventArgs<TEntity>(entity, changeType));
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (this.isDisposed is false)
            {
                if (disposing)
                {
                    this.Context.Dispose();
                }
                this.isDisposed = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}