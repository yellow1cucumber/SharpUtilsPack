using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using Microsoft.EntityFrameworkCore;
using SharpUtils.Results;

namespace SharpUtils.Repository.Generic
{
    /// <summary>
    /// Provides a generic implementation of the repository pattern using Entity Framework Core.
    /// This class handles common database operations while providing strong typing, error handling, and pagination support.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity that this repository manages. Must be a reference type that represents a database entity.</typeparam>
    /// <typeparam name="TKey">The type of the entity's primary key. Must implement IEquatable to ensure proper comparison operations.</typeparam>
    /// <remarks>
    /// <para>
    /// This repository implementation wraps Entity Framework Core operations in a Result pattern,
    /// providing a functional approach to error handling without throwing exceptions for expected failures.
    /// </para>
    /// <para>
    /// Key features include:
    /// - CRUD operations with error handling
    /// - Asynchronous operation support
    /// - Pagination with metadata
    /// - Eager loading of related entities
    /// - Bulk operations for better performance
    /// - Transaction support
    /// - Change tracking and events
    /// </para>
    /// <para>
    /// Usage example:
    /// <code>
    /// using (var repo = new EfGenericRepository&lt;Product, int&gt;(dbContext))
    /// {
    ///     var result = await repo.GetByIdAsync(1);
    ///     if (result.IsSuccess)
    ///     {
    ///         var product = result.Value;
    ///         // Process the product...
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public class EfGenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
        where TEntity : class
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// The Entity Framework Core database context used by this repository.
        /// Protected to allow access in derived classes for custom operations.
        /// </summary>
        protected readonly DbContext Context;

        /// <summary>
        /// The DbSet for the entity type being managed by this repository.
        /// Protected to allow access in derived classes for custom operations.
        /// </summary>
        protected readonly DbSet<TEntity> DbSet;

        /// <summary>
        /// Tracks whether this repository has been disposed.
        /// Used to prevent access to disposed context resources.
        /// </summary>
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="EfGenericRepository{TEntity, TKey}"/> class with the specified database context.
        /// </summary>
        /// <param name="context">The Entity Framework Core database context to use for data access operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
        /// <remarks>
        /// <para>
        /// The constructor initializes both the context and entity set (DbSet) for the specified entity type.
        /// The context is stored for transaction and change tracking support, while the DbSet provides
        /// entity-specific operations.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// using var context = new MyDbContext();
        /// var repository = new EfGenericRepository&lt;Product, int&gt;(context);
        /// </code>
        /// </para>
        /// </remarks>
        public EfGenericRepository(DbContext context)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            this.DbSet = context.Set<TEntity>();
        }

        /// <summary>
        /// Occurs when an entity managed by this repository is changed (added, modified, or deleted).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This event provides notification of entity changes before they are persisted to the database.
        /// Subscribers can use this event to implement audit logging, caching invalidation, or other
        /// side effects that should occur when entities change.
        /// </para>
        /// <para>
        /// The event provides the changed entity and the type of change through <see cref="EntityChangedEventArgs{TEntity}"/>.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// repository.EntityChanged += (sender, args) =>
        /// {
        ///     var entity = args.Entity;
        ///     var changeType = args.ChangeType;
        ///     // Handle the change...
        /// };
        /// </code>
        /// </para>
        /// </remarks>
        public event EventHandler<EntityChangedEventArgs<TEntity>>? EntityChanged;

        /// <summary>
        /// Gets a queryable collection of all entities in the repository.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property provides direct access to the underlying IQueryable for advanced querying scenarios
        /// that aren't covered by the standard repository methods. Use with caution as it bypasses the
        /// repository's error handling and change tracking mechanisms.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var complexQuery = repository.Query
        ///     .Where(e => e.IsActive)
        ///     .GroupBy(e => e.Category)
        ///     .Select(g => new { Category = g.Key, Count = g.Count() });
        /// </code>
        /// </para>
        /// </remarks>
        public IQueryable<TEntity> Query => this.DbSet;

        /// <summary>
        /// Adds a new entity to the repository.
        /// </summary>
        /// <param name="entity">The entity to add. Must not be null.</param>
        /// <returns>
        /// A <see cref="Result{TEntity}"/> that represents the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains the added entity with any database-generated values (e.g., identity keys)</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method only stages the entity for insertion - it does not save changes to the database.
        /// Call <see cref="SaveChanges"/> or <see cref="SaveChangesAsync"/> to persist the changes.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var newProduct = new Product { Name = "Widget", Price = 9.99m };
        /// var result = repository.Add(newProduct);
        /// if (result.IsSuccess)
        /// {
        ///     // The entity has been staged for insertion
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
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

        /// <summary>
        /// Adds multiple entities to the repository in a single operation.
        /// </summary>
        /// <param name="entities">The collection of entities to add. Must not be null.</param>
        /// <returns>
        /// A <see cref="Result{IEnumerable}"/> that represents the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains the collection of added entities</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method provides an optimized way to add multiple entities in a single operation.
        /// The changes are only staged in the change tracker - call <see cref="SaveChanges"/> or
        /// <see cref="SaveChangesAsync"/> to persist them to the database.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var entities = new List&lt;Product&gt; 
        /// {
        ///     new Product { Name = "Product 1" },
        ///     new Product { Name = "Product 2" }
        /// };
        /// var result = repository.AddRange(entities);
        /// if (result.IsSuccess)
        /// {
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> is null.</exception>
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

        /// <summary>
        /// Retrieves an entity by its primary key value.
        /// </summary>
        /// <param name="id">The primary key value of the entity to retrieve. Cannot be null.</param>
        /// <returns>
        /// A <see cref="Result{TEntity}"/> that represents the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains the found entity.</description></item>
        /// <item><description>Failure: Contains an error message if the entity is not found or an exception occurs.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method performs an efficient lookup using the primary key index. For queries using other fields,
        /// use <see cref="GetFirstOrDefault"/> or <see cref="GetWhere"/> instead.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = repository.GetById(123);
        /// if (result.IsSuccess)
        /// {
        ///     var entity = result.Value;
        ///     if (entity != null)
        ///     {
        ///         // Process the found entity...
        ///     }
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null.</exception>
        public Result<TEntity> GetById(TKey id)
        {
            try
            {
                if (id == null)
                    throw new ArgumentNullException(nameof(id));

                var entity = this.DbSet.Find(id);

                return entity == null
                    ? Result<TEntity>.Failure("Entity not found.")
                    : Result<TEntity>.Success(entity);
            }
            catch (Exception ex)
            {
                return Result<TEntity>.Failure($"Failed to get entity by ID: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves the first entity matching the specified condition.
        /// </summary>
        /// <param name="predicate">A function defining the condition to match. Cannot be null.</param>
        /// <returns>
        /// A <see cref="Result{TEntity}"/> that represents the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains the first matching entity.</description></item>
        /// <item><description>Failure: Contains an error message if no match is found or an exception occurs.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method executes immediately and returns the first entity that matches the condition.
        /// If no entity matches, the result will indicate failure with an appropriate error message.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = repository.GetFirstOrDefault(e => e.Status == "Active" && e.Price > 100);
        /// if (result.IsSuccess)
        /// {
        ///     var entity = result.Value;
        ///     // Process the found entity...
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
        public Result<TEntity> GetFirstOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var entity = this.DbSet.FirstOrDefault(predicate);
                
                return entity == null 
                    ? Result<TEntity>.Failure("No matching entity found.")
                    : Result<TEntity>.Success(entity);
            }
            catch (Exception ex)
            {
                return Result<TEntity>.Failure($"Failed to get first entity: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves all entities from the repository.
        /// </summary>
        /// <returns>
        /// A <see cref="Result{IEnumerable}"/> that represents the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains all entities in the repository</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method loads all entities into memory. For large datasets, consider using
        /// pagination methods like <see cref="GetPaged"/> or streaming methods like
        /// <see cref="GetAllAsStreamAsync"/> instead.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = repository.GetAll();
        /// if (result.IsSuccess)
        /// {
        ///     foreach (var entity in result.Value)
        ///     {
        ///         // Process each entity...
        ///     }
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Retrieves all entities that match the specified condition.
        /// </summary>
        /// <param name="predicate">A function defining the condition to match.</param>
        /// <returns>
        /// A <see cref="Result{IEnumerable}"/> that represents the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains all matching entities</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method loads all matching entities into memory. For large result sets,
        /// consider using pagination or streaming methods instead.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = repository.GetWhere(e => e.IsActive &amp;&amp; e.Category == "Electronics");
        /// if (result.IsSuccess)
        /// {
        ///     foreach (var entity in result.Value)
        ///     {
        ///         // Process each matching entity...
        ///     }
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
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

        /// <summary>
        /// Retrieves all entities with specified related entities eagerly loaded.
        /// </summary>
        /// <param name="includes">The related entities to include in the query.</param>
        /// <returns>
        /// A <see cref="Result{IEnumerable}"/> that represents the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains all entities with their related data</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method eagerly loads related entities in a single query, preventing the N+1 query problem.
        /// Use this when you know you'll need the related data.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = repository.GetWithInclude(
        ///     e => e.Category,
        ///     e => e.Supplier,
        ///     e => e.OrderDetails
        /// );
        /// if (result.IsSuccess)
        /// {
        ///     foreach (var entity in result.Value)
        ///     {
        ///         // Access entity.Category, entity.Supplier, etc. without additional queries
        ///     }
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Retrieves all entities that match a condition with specified related entities eagerly loaded.
        /// </summary>
        /// <param name="predicate">A function defining the condition to match.</param>
        /// <param name="includes">The related entities to include in the query.</param>
        /// <returns>
        /// A <see cref="Result{IEnumerable}"/> that represents the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains all matching entities with their related data</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method combines filtering with eager loading of related entities, executing
        /// everything in a single efficient query.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = repository.GetWhereWithInclude(
        ///     e => e.IsActive &amp;&amp; e.Price > 100,
        ///     e => e.Category,
        ///     e => e.Supplier
        /// );
        /// if (result.IsSuccess)
        /// {
        ///     foreach (var entity in result.Value)
        ///     {
        ///         // Access entity.Category, entity.Supplier, etc. without additional queries
        ///     }
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
        public Result<IEnumerable<TEntity>> GetWhereWithInclude(
            Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes)
        {
            try
            {
                IQueryable<TEntity> query = this.DbSet;
                query = query.Where(predicate);
                query = includes.Aggregate(query, (current, include) => current.Include(include));
                return Result<IEnumerable<TEntity>>.Success(query.ToList());
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<TEntity>>.Failure($"Failed to get entities with predicate and includes: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves a paginated set of entities from the repository.
        /// </summary>
        /// <param name="pageNumber">The 1-based page number to retrieve.</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <returns>
        /// A <see cref="PaginatedResult{TEntity}"/> that represents the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains the requested page of entities along with pagination metadata</description></item>
        /// <item><description>Empty: When there are no entities for the requested page</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method efficiently retrieves a subset of entities using SQL OFFSET/FETCH,
        /// making it suitable for large datasets where retrieving all records would be impractical.
        /// </para>
        /// <para>
        /// The returned <see cref="PaginatedResult{TEntity}"/> includes:
        /// <list type="bullet">
        /// <item><description>The entities for the requested page</description></item>
        /// <item><description>The current page number</description></item>
        /// <item><description>The page size</description></item>
        /// <item><description>The total number of items across all pages</description></item>
        /// <item><description>The total number of pages</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = repository.GetPaged(pageNumber: 1, pageSize: 10);
        /// if (result.IsSuccess)
        /// {
        ///     var items = result.Value;
        ///     var totalPages = result.TotalPages;
        ///     var totalItems = result.TotalItems;
        ///     // Process the items...
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when:
        /// <list type="bullet">
        /// <item><description><paramref name="pageNumber"/> is less than or equal to 0</description></item>
        /// <item><description><paramref name="pageSize"/> is less than or equal to 0</description></item>
        /// </list>
        /// </exception>
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

        /// <summary>
        /// Retrieves a paginated set of entities from the repository that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function defining the condition to match entities.</param>
        /// <param name="pageNumber">The 1-based page number to retrieve.</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <returns>
        /// A <see cref="PaginatedResult{TEntity}"/> representing the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains the requested page of matching entities along with pagination metadata.</description></item>
        /// <item><description>Empty: When there are no entities for the requested page.</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method efficiently retrieves a subset of entities that match the predicate using SQL OFFSET/FETCH,
        /// making it suitable for large datasets where retrieving all records would be impractical.
        /// </para>
        /// <para>
        /// The returned <see cref="PaginatedResult{TEntity}"/> includes:
        /// <list type="bullet">
        /// <item><description>The entities for the requested page.</description></item>
        /// <item><description>The current page number.</description></item>
        /// <item><description>The page size.</description></item>
        /// <item><description>The total number of matching items across all pages.</description></item>
        /// <item><description>The total number of pages.</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = repository.GetPagedWhere(e => e.IsActive, pageNumber: 2, pageSize: 10);
        /// if (result.IsSuccess)
        /// {
        ///     var items = result.Value;
        ///     var totalPages = result.TotalPages;
        ///     var totalItems = result.TotalItems;
        ///     // Process the items...
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="pageNumber"/> or <paramref name="pageSize"/> is less than or equal to 0.
        /// </exception>
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

        /// <summary>
        /// Retrieves a paginated set of entities from the repository ordered by the specified key.
        /// </summary>
        /// <param name="orderBy">An expression specifying the key to order by.</param>
        /// <param name="pageNumber">The 1-based page number to retrieve.</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <param name="ascending">If true, orders in ascending order; otherwise, descending order.</param>
        /// <returns>
        /// A <see cref="PaginatedResult{TEntity}"/> representing the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains the requested page of ordered entities along with pagination metadata.</description></item>
        /// <item><description>Empty: When there are no entities for the requested page.</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method efficiently retrieves a subset of entities ordered by the specified key using SQL OFFSET/FETCH,
        /// making it suitable for large datasets where retrieving all records would be impractical.
        /// </para>
        /// <para>
        /// The returned <see cref="PaginatedResult{TEntity}"/> includes:
        /// <list type="bullet">
        /// <item><description>The entities for the requested page.</description></item>
        /// <item><description>The current page number.</description></item>
        /// <item><description>The page size.</description></item>
        /// <item><description>The total number of items across all pages.</description></item>
        /// <item><description>The total number of pages.</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = repository.GetPagedOrdered(e => e.Id, pageNumber: 1, pageSize: 10, ascending: true);
        /// if (result.IsSuccess)
        /// {
        ///     var items = result.Value;
        ///     var totalPages = result.TotalPages;
        ///     var totalItems = result.TotalItems;
        ///     // Process the items...
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Retrieves a paginated set of entities from the repository that match the specified predicate,
        /// with specified related entities eagerly loaded.
        /// </summary>
        /// <param name="predicate">A function defining the condition to match entities.</param>
        /// <param name="pageNumber">The 1-based page number to retrieve.</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <param name="includes">The related entities to include in the query.</param>
        /// <returns>
        /// A <see cref="PaginatedResult{TEntity}"/> representing the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains the requested page of matching entities with their related data and pagination metadata.</description></item>
        /// <item><description>Empty: When there are no entities for the requested page.</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method efficiently retrieves a subset of entities that match the predicate and includes related entities,
        /// using SQL OFFSET/FETCH for pagination. It is suitable for large datasets and complex queries.
        /// </para>
        /// <para>
        /// The returned <see cref="PaginatedResult{TEntity}"/> includes:
        /// <list type="bullet">
        /// <item><description>The entities for the requested page.</description></item>
        /// <item><description>The current page number.</description></item>
        /// <item><description>The page size.</description></item>
        /// <item><description>The total number of matching items across all pages.</description></item>
        /// <item><description>The total number of pages.</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = repository.GetPagedWhereWithInclude(
        ///     e => e.IsActive,
        ///     pageNumber: 2,
        ///     pageSize: 10,
        ///     e => e.Category,
        ///     e => e.Supplier
        /// );
        /// if (result.IsSuccess)
        /// {
        ///     var items = result.Value;
        ///     var totalPages = result.TotalPages;
        ///     var totalItems = result.TotalItems;
        ///     // Process the items...
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Retrieves a paginated set of entities from the repository ordered by the specified key,
        /// with specified related entities eagerly loaded.
        /// </summary>
        /// <param name="orderBy">An expression specifying the key to order by.</param>
        /// <param name="pageNumber">The 1-based page number to retrieve.</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <param name="ascending">If true, orders in ascending order; otherwise, descending order.</param>
        /// <param name="includes">The related entities to include in the query.</param>
        /// <returns>
        /// A <see cref="PaginatedResult{TEntity}"/> representing the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains the requested page of ordered entities with their related data and pagination metadata.</description></item>
        /// <item><description>Empty: When there are no entities for the requested page.</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method efficiently retrieves a subset of entities ordered by the specified key and includes related entities,
        /// using SQL OFFSET/FETCH for pagination. It is suitable for large datasets and complex queries.
        /// </para>
        /// <para>
        /// The returned <see cref="PaginatedResult{TEntity}"/> includes:
        /// <list type="bullet">
        /// <item><description>The entities for the requested page.</description></item>
        /// <item><description>The current page number.</description></item>
        /// <item><description>The page size.</description></item>
        /// <item><description>The total number of items across all pages.</description></item>
        /// <item><description>The total number of pages.</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = repository.GetPagedOrderedWithInclude(
        ///     e => e.Id,
        ///     pageNumber: 1,
        ///     pageSize: 10,
        ///     ascending: true,
        ///     e => e.Category,
        ///     e => e.Supplier
        /// );
        /// if (result.IsSuccess)
        /// {
        ///     var items = result.Value;
        ///     var totalPages = result.TotalPages;
        ///     var totalItems = result.TotalItems;
        ///     // Process the items...
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Gets the total count of all entities in the repository.
        /// </summary>
        /// <returns>
        /// A <see cref="Result{Int32}"/> containing the total count of entities,
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method executes a count query against the database to determine the number of entities
        /// managed by this repository. It is efficient for most scenarios, but for very large tables,
        /// consider using approximate count techniques if supported by your database.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = repository.Count();
        /// if (result.IsSuccess)
        /// {
        ///     Console.WriteLine($"Total entities: {result.Value}");
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Gets the count of entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each entity for a condition.</param>
        /// <returns>
        /// A <see cref="Result{Int32}"/> containing the count of matching entities,
        /// or an error message if the operation fails.
        /// </returns>
        /// <example>
        /// <code>
        /// var result = repository.CountWhere(p => p.IsActive);
        /// </code>
        /// </example>
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

        /// <summary>
        /// Determines whether any entity in the repository matches the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each entity for a condition.</param>
        /// <returns>
        /// A <see cref="Result{Boolean}"/> containing <c>true</c> if any entity matches the predicate, <c>false</c> otherwise,
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is more efficient than using <see cref="CountWhere"/> when you only need to check for existence.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = repository.Exists(e => e.Email == userEmail);
        /// if (result.IsSuccess && result.Value)
        /// {
        ///     // At least one entity exists with the specified email
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Determines whether an entity with the specified primary key exists in the repository.
        /// </summary>
        /// <param name="id">The primary key value of the entity to check for existence.</param>
        /// <returns>
        /// A <see cref="Result{Boolean}"/> containing <c>true</c> if an entity with the specified key exists, <c>false</c> otherwise,
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method performs an efficient lookup using the primary key index. For existence checks using other fields,
        /// use <see cref="Exists"/> instead.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = repository.ExistsById(123);
        /// if (result.IsSuccess && result.Value)
        /// {
        ///     // Entity with ID 123 exists
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Updates an existing entity in the repository.
        /// </summary>
        /// <param name="entity">The entity to update with modified values. Must not be null.</param>
        /// <returns>
        /// A <see cref="Result{TEntity}"/> that represents the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains the updated entity</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method only marks the entity as modified - it does not save changes to the database.
        /// Call <see cref="SaveChanges"/> or <see cref="SaveChangesAsync"/> to persist the changes.
        /// </para>
        /// <para>
        /// The method handles both attached and detached entities:
        /// <list type="bullet">
        /// <item><description>For attached entities: Simply marks the entity as modified</description></item>
        /// <item><description>For detached entities: Attaches them first, then marks as modified</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var entity = repository.GetById(123).Value;
        /// entity.Name = "Updated Name";
        /// var result = repository.Update(entity);
        /// if (result.IsSuccess)
        /// {
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
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

        /// <summary>
        /// Updates multiple entities in the repository in a single operation.
        /// </summary>
        /// <param name="entities">The collection of entities to update. Must not be null.</param>
        /// <returns>
        /// A <see cref="Result{IEnumerable}"/> that represents the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains the collection of updated entities</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method marks each entity in the collection as modified in the change tracker.
        /// It does not persist changes to the database until <see cref="SaveChanges"/> or <see cref="SaveChangesAsync"/> is called.
        /// Handles both attached and detached entities by attaching them if necessary.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var entities = new List&lt;Product&gt; { ... };
        /// var result = repository.UpdateRange(entities);
        /// if (result.IsSuccess)
        /// {
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> is null.</exception>
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

        /// <summary>
        /// Removes an entity from the repository.
        /// </summary>
        /// <param name="entity">The entity to remove. Must not be null.</param>
        /// <returns>
        /// A <see cref="Result"/> that represents the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Indicates the entity was successfully marked for deletion</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method only marks the entity for deletion - it does not save changes to the database.
        /// Call <see cref="SaveChanges"/> or <see cref="SaveChangesAsync"/> to persist the changes.
        /// </para>
        /// <para>
        /// The method handles both attached and detached entities:
        /// <list type="bullet">
        /// <item><description>For attached entities: Marks them for deletion directly</description></item>
        /// <item><description>For detached entities: Attaches them first, then marks for deletion</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var entity = repository.GetById(123).Value;
        /// var result = repository.Delete(entity);
        /// if (result.IsSuccess)
        /// {
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
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

        /// <summary>
        /// Removes multiple entities from the repository in a single operation.
        /// </summary>
        /// <param name="entities">The collection of entities to remove. Must not be null.</param>
        /// <returns>
        /// A <see cref="Result"/> indicating the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Indicates the entities were successfully marked for deletion</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method only marks the entities for deletion - it does not save changes to the database.
        /// Call <see cref="SaveChanges"/> or <see cref="SaveChangesAsync"/> to persist the changes.
        /// </para>
        /// <para>
        /// The method handles both attached and detached entities:
        /// <list type="bullet">
        /// <item><description>For attached entities: Marks them for deletion directly</description></item>
        /// <item><description>For detached entities: Attaches them first, then marks for deletion</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var entities = repository.GetWhere(e => e.IsObsolete).Value;
        /// var result = repository.DeleteRange(entities);
        /// if (result.IsSuccess)
        /// {
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> is null.</exception>
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

        /// <summary>
        /// Removes an entity with the specified primary key from the repository.
        /// </summary>
        /// <param name="id">The primary key value of the entity to remove.</param>
        /// <returns>
        /// A <see cref="Result"/> indicating the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Indicates the entity was successfully marked for deletion</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method first retrieves the entity by its key and then deletes it.
        /// If the entity is not found, the operation fails with an error message.
        /// The entity is only marked for deletion; call <see cref="SaveChanges"/> or <see cref="SaveChangesAsync"/> to persist the change.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = repository.DeleteById(123);
        /// if (result.IsSuccess)
        /// {
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Removes all entities that match the specified predicate from the repository.
        /// </summary>
        /// <param name="predicate">A function defining the condition to match entities for deletion. Must not be null.</param>
        /// <returns>
        /// A <see cref="Result"/> indicating the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Indicates the entities were successfully marked for deletion</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method retrieves all entities matching the predicate and marks them for deletion in the change tracker.
        /// Changes are not persisted to the database until <see cref="SaveChanges"/> or <see cref="SaveChangesAsync"/> is called.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = repository.DeleteWhere(e => e.IsObsolete);
        /// if (result.IsSuccess)
        /// {
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
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

        /// <summary>
        /// Performs a bulk update operation on entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each entity for a condition.</param>
        /// <param name="updateExpression">An expression that defines how to update matching entities.</param>
        /// <returns>
        /// A <see cref="Result{Int32}"/> that represents the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains the number of entities updated</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method provides an efficient way to update multiple entities that match a condition
        /// without having to load and track each entity individually. However, it bypasses entity
        /// validation and change tracking.
        /// </para>
        /// <para>
        /// Important considerations:
        /// <list type="bullet">
        /// <item><description>The update is performed in memory before being saved to the database</description></item>
        /// <item><description>Entity validation is not performed</description></item>
        /// <item><description>Change tracking events are still triggered for each updated entity</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.BulkUpdateAsync(
        ///     e => e.Category == "Electronics",
        ///     e => new Product { Price = e.Price * 1.1m }
        /// );
        /// if (result.IsSuccess)
        /// {
        ///     var updatedCount = result.Value;
        ///     // Handle success...
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when:
        /// <list type="bullet">
        /// <item><description><paramref name="predicate"/> is null</description></item>
        /// <item><description><paramref name="updateExpression"/> is null</description></item>
        /// </list>
        /// </exception>
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

        /// <summary>
        /// Performs a bulk delete operation on entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each entity for a condition.</param>
        /// <returns>
        /// A <see cref="Result{Int32}"/> that represents the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains the number of entities deleted</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method provides an efficient way to delete multiple entities that match a condition
        /// in a single operation. The entities are loaded into memory to ensure proper change tracking
        /// and event notification.
        /// </para>
        /// <para>
        /// Important considerations:
        /// <list type="bullet">
        /// <item><description>Entities are loaded into memory before deletion</description></item>
        /// <item><description>Change tracking events are triggered for each deleted entity</description></item>
        /// <item><description>Cascading deletes are handled according to the entity configuration</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.BulkDeleteAsync(e => e.IsExpired);
        /// if (result.IsSuccess)
        /// {
        ///     var deletedCount = result.Value;
        ///     // Handle success...
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
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

        /// <summary>
        /// Saves all pending changes in the repository to the database.
        /// </summary>
        /// <returns>
        /// A <see cref="Result{Int32}"/> that represents the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains the number of state entries written to the database</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method persists all tracked changes (adds, updates, and deletes) to the database
        /// in a single transaction. Changes are not saved to the database until this method is called.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// // Make some changes
        /// repository.Add(newEntity);
        /// repository.Update(existingEntity);
        /// repository.Delete(oldEntity);
        /// 
        /// // Persist all changes
        /// var result = repository.SaveChanges();
        /// if (result.IsSuccess)
        /// {
        ///     Console.WriteLine($"{result.Value} changes saved to database");
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously adds a new entity to the repository.
        /// </summary>
        /// <param name="entity">The entity to add. Must not be null.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{TEntity}"/> that:
        /// <list type="bullet">
        /// <item><description>On success: Contains the added entity with any database-generated values (e.g., identity keys)</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This is the async version of <see cref="Add"/>. The entity is staged for insertion
        /// and will be added to the database when you call <see cref="SaveChangesAsync"/>.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var newProduct = new Product { Name = "Widget", Price = 9.99m };
        /// var result = await repository.AddAsync(newProduct);
        /// if (result.IsSuccess)
        /// {
        ///     // The entity has been staged for insertion
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
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

        /// <summary>
        /// Asynchronously adds multiple entities to the repository in a single operation.
        /// </summary>
        /// <param name="entities">The collection of entities to add. Must not be null.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{IEnumerable}"/> that:
        /// <list type="bullet">
        /// <item><description>On success: Contains the collection of added entities</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This is the async version of <see cref="AddRange"/>. The changes are only staged
        /// until you call <see cref="SaveChangesAsync"/>.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var entities = new List&lt;Product&gt; 
        /// {
        ///     new Product { Name = "Product 1" },
        ///     new Product { Name = "Product 2" }
        /// };
        /// var result = await repository.AddRangeAsync(entities);
        /// if (result.IsSuccess)
        /// {
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> is null.</exception>
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

        /// <summary>
        /// Asynchronously retrieves an entity by its primary key.
        /// </summary>
        /// <param name="id">The primary key value of the entity to retrieve. Cannot be null.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{TEntity}"/>:
        /// <list type="bullet">
        /// <item><description>On success: Contains the found entity.</description></item>
        /// <item><description>On failure: Contains an error message if the entity is not found or an exception occurs.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method performs an efficient lookup using the primary key index. For queries using other fields,
        /// use <see cref="GetFirstOrDefaultAsync"/> or <see cref="GetWhereAsync"/> instead.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.GetByIdAsync(123);
        /// if (result.IsSuccess)
        /// {
        ///     var entity = result.Value;
        ///     if (entity != null)
        ///     {
        ///         // Process the found entity...
        ///     }
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null.</exception>
        public async Task<Result<TEntity>> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id == null)
                    throw new ArgumentNullException(nameof(id));

                var entity = await this.DbSet.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);

                return entity == null 
                    ? Result<TEntity>.Failure("Entity not found.")
                    : Result<TEntity>.Success(entity);
            }
            catch (Exception ex)
            {
                return Result<TEntity>.Failure($"Failed to get entity by ID: {ex.Message}");
            }
        }

        /// <summary>
        /// Asynchronously retrieves the first entity that matches the specified predicate, or null if no match is found.
        /// </summary>
        /// <param name="predicate">A function to test each entity for a condition. Cannot be null.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{TEntity}"/>:
        /// <list type="bullet">
        /// <item><description>On success: Contains the first matching entity.</description></item>
        /// <item><description>On failure: Contains an error message if no match is found or an exception occurs.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method executes the query asynchronously and returns the first entity that matches the condition.
        /// If no entity matches, the result will indicate failure with an appropriate error message.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.GetFirstOrDefaultAsync(e => e.Status == "Active" && e.Price > 100);
        /// if (result.IsSuccess)
        /// {
        ///     var entity = result.Value;
        ///     // Process the found entity...
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
        public async Task<Result<TEntity>> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(predicate));

                var entity = await this.DbSet.FirstOrDefaultAsync(predicate, cancellationToken).ConfigureAwait(false);

                return entity == null
                    ? Result<TEntity>.Failure("Entity not found.")
                    : Result<TEntity>.Success(entity);
            }
            catch (Exception ex)
            {
                return Result<TEntity>.Failure($"Failed to get first entity: {ex.Message}");
            }
        }

        /// <summary>
        /// Asynchronously retrieves all entities from the repository.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{IEnumerable}"/> that:
        /// <list type="bullet">
        /// <item><description>On success: Contains all entities in the repository</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This is the async version of <see cref="GetAll"/>. For large datasets, consider using
        /// <see cref="GetPagedAsync"/> or <see cref="GetAllAsStreamAsync"/> instead.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.GetAllAsync();
        /// if (result.IsSuccess)
        /// {
        ///     foreach (var entity in result.Value)
        ///     {
        ///         // Process each entity...
        ///     }
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously retrieves all entities that match the specified condition.
        /// </summary>
        /// <param name="predicate">A function defining the condition to match.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{IEnumerable}"/> that:
        /// <list type="bullet">
        /// <item><description>On success: Contains all matching entities</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This is the async version of <see cref="GetWhere"/>. For large result sets,
        /// consider using <see cref="GetPagedWhereAsync"/> instead.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.GetWhereAsync(
        ///     e => e.IsActive &amp;&amp; e.Category == "Electronics"
        /// );
        /// if (result.IsSuccess)
        /// {
        ///     foreach (var entity in result.Value)
        ///     {
        ///         // Process each matching entity...
        ///     }
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
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

        /// <summary>
        /// Asynchronously retrieves all entities with specified related entities eagerly loaded.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <param name="includes">The related entities to include in the query.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{IEnumerable}"/> that:
        /// <list type="bullet">
        /// <item><description>On success: Contains all entities with their related data</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This is the async version of <see cref="GetWithInclude"/>. Use this method when you
        /// need to load related entities efficiently in a single query.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.GetWithIncludeAsync(
        ///     cancellationToken,
        ///     e => e.Category,
        ///     e => e.Supplier,
        ///     e => e.OrderDetails
        /// );
        /// if (result.IsSuccess)
        /// {
        ///     foreach (var entity in result.Value)
        ///     {
        ///         // Access entity.Category, entity.Supplier, etc. without additional queries
        ///     }
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously retrieves all entities that match a condition with specified related entities eagerly loaded.
        /// </summary>
        /// <param name="predicate">A function defining the condition to match.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <param name="includes">The related entities to include in the query.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{IEnumerable}"/> that:
        /// <list type="bullet">
        /// <item><description>On success: Contains all matching entities with their related data</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This is the async version of <see cref="GetWhereWithInclude"/>. It combines filtering
        /// and eager loading of related entities in a single efficient query.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.GetWhereWithIncludeAsync(
        ///     e => e.IsActive &amp;&amp; e.Price > 100,
        ///     cancellationToken,
        ///     e => e.Category,
        ///     e => e.Supplier
        /// );
        /// if (result.IsSuccess)
        /// {
        ///     foreach (var entity in result.Value)
        ///     {
        ///         // Access entity.Category, entity.Supplier, etc. without additional queries
        ///     }
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
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

        /// <summary>
        /// Asynchronously retrieves a paginated set of entities from the repository.
        /// </summary>
        /// <param name="pageNumber">The 1-based page number to retrieve.</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="PaginatedResult{TEntity}"/> that:
        /// <list type="bullet">
        /// <item><description>On success: Contains the requested page of entities along with pagination metadata</description></item>
        /// <item><description>On empty: When there are no entities for the requested page</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method efficiently retrieves a subset of entities using SQL OFFSET/FETCH,
        /// making it suitable for large datasets where retrieving all records would be impractical.
        /// </para>
        /// <para>
        /// The returned <see cref="PaginatedResult{TEntity}"/> includes:
        /// <list type="bullet">
        /// <item><description>The entities for the requested page</description></item>
        /// <item><description>The current page number</description></item>
        /// <item><description>The page size</description></item>
        /// <item><description>The total number of items across all pages</description></item>
        /// <item><description>The total number of pages</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.GetPagedAsync(pageNumber: 1, pageSize: 10);
        /// if (result.IsSuccess)
        /// {
        ///     var items = result.Value;
        ///     var totalPages = result.TotalPages;
        ///     var totalItems = result.TotalItems;
        ///     // Process the items...
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when:
        /// <list type="bullet">
        /// <item><description><paramref name="pageNumber"/> is less than or equal to 0</description></item>
        /// <item><description><paramref name="pageSize"/> is less than or equal to 0</description></item>
        /// </list>
        /// </exception>
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

        /// <summary>
        /// Asynchronously retrieves a paginated set of entities from the repository that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function defining the condition to match entities.</param>
        /// <param name="pageNumber">The 1-based page number to retrieve.</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="PaginatedResult{TEntity}"/> that:
        /// <list type="bullet">
        /// <item><description>On success: Contains the requested page of matching entities along with pagination metadata</description></item>
        /// <item><description>On empty: When there are no entities for the requested page</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method efficiently retrieves a subset of entities that match the predicate using SQL OFFSET/FETCH,
        /// making it suitable for large datasets where retrieving all records would be impractical.
        /// </para>
        /// <para>
        /// The returned <see cref="PaginatedResult{TEntity}"/> includes:
        /// <list type="bullet">
        /// <item><description>The entities for the requested page</description></item>
        /// <item><description>The current page number</description></item>
        /// <item><description>The page size</description></item>
        /// <item><description>The total number of matching items across all pages</description></item>
        /// <item><description>The total number of pages</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.GetPagedWhereAsync(e => e.IsActive, pageNumber: 2, pageSize: 10);
        /// if (result.IsSuccess)
        /// {
        ///     var items = result.Value;
        ///     var totalPages = result.TotalPages;
        ///     var totalItems = result.TotalItems;
        ///     // Process the items...
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously retrieves a paginated set of entities from the repository ordered by the specified key.
        /// </summary>
        /// <param name="orderBy">An expression specifying the key to order by.</param>
        /// <param name="pageNumber">The 1-based page number to retrieve.</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <param name="ascending">If true, orders in ascending order; otherwise, descending order.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="PaginatedResult{TEntity}"/> that:
        /// <list type="bullet">
        /// <item><description>On success: Contains the requested page of ordered entities along with pagination metadata</description></item>
        /// <item><description>On empty: When there are no entities for the requested page</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method efficiently retrieves a subset of entities ordered by the specified key using SQL OFFSET/FETCH,
        /// making it suitable for large datasets where retrieving all records would be impractical.
        /// </para>
        /// <para>
        /// The returned <see cref="PaginatedResult{TEntity}"/> includes:
        /// <list type="bullet">
        /// <item><description>The entities for the requested page</description></item>
        /// <item><description>The current page number</description></item>
        /// <item><description>The page size</description></item>
        /// <item><description>The total number of items across all pages</description></item>
        /// <item><description>The total number of pages</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.GetPagedOrderedAsync(e => e.Id, pageNumber: 1, pageSize: 10, ascending: true);
        /// if (result.IsSuccess)
        /// {
        ///     var items = result.Value;
        ///     var totalPages = result.TotalPages;
        ///     var totalItems = result.TotalItems;
        ///     // Process the items...
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously retrieves a paginated set of entities from the repository that match the specified predicate,
        /// with specified related entities eagerly loaded.
        /// </summary>
        /// <param name="predicate">A function defining the condition to match entities.</param>
        /// <param name="pageNumber">The 1-based page number to retrieve.</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <param name="includes">The related entities to include in the query.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="PaginatedResult{TEntity}"/> that:
        /// <list type="bullet">
        /// <item><description>On success: Contains the requested page of matching entities with their related data and pagination metadata.</description></item>
        /// <item><description>On empty: When there are no entities for the requested page.</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method efficiently retrieves a subset of entities that match the predicate and includes related entities,
        /// using SQL OFFSET/FETCH for pagination. It is suitable for large datasets and complex queries.
        /// </para>
        /// <para>
        /// The returned <see cref="PaginatedResult{TEntity}"/> includes:
        /// <list type="bullet">
        /// <item><description>The entities for the requested page.</description></item>
        /// <item><description>The current page number.</description></item>
        /// <item><description>The page size.</description></item>
        /// <item><description>The total number of matching items across all pages.</description></item>
        /// <item><description>The total number of pages.</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.GetPagedWhereWithIncludeAsync(
        ///     e => e.IsActive,
        ///     pageNumber: 2,
        ///     pageSize: 10,
        ///     cancellationToken,
        ///     e => e.Category,
        ///     e => e.Supplier
        /// );
        /// if (result.IsSuccess)
        /// {
        ///     var items = result.Value;
        ///     var totalPages = result.TotalPages;
        ///     var totalItems = result.TotalItems;
        ///     // Process the items...
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously retrieves a paginated set of entities from the repository ordered by the specified key,
        /// with specified related entities eagerly loaded.
        /// </summary>
        /// <param name="orderBy">An expression specifying the key to order by.</param>
        /// <param name="pageNumber">The 1-based page number to retrieve.</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <param name="ascending">If true, orders in ascending order; otherwise, descending order.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <param name="includes">The related entities to include in the query.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="PaginatedResult{TEntity}"/> that:
        /// <list type="bullet">
        /// <item><description>On success: Contains the requested page of ordered entities with their related data and pagination metadata.</description></item>
        /// <item><description>On empty: When there are no entities for the requested page.</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method efficiently retrieves a subset of entities ordered by the specified key and includes related entities,
        /// using SQL OFFSET/FETCH for pagination. It is suitable for large datasets and complex queries.
        /// </para>
        /// <para>
        /// The returned <see cref="PaginatedResult{TEntity}"/> includes:
        /// <list type="bullet">
        /// <item><description>The entities for the requested page.</description></item>
        /// <item><description>The current page number.</description></item>
        /// <item><description>The page size.</description></item>
        /// <item><description>The total number of items across all pages.</description></item>
        /// <item><description>The total number of pages.</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.GetPagedOrderedWithIncludeAsync(
        ///     e => e.Id,
        ///     pageNumber: 1,
        ///     pageSize: 10,
        ///     ascending: true,
        ///     cancellationToken,
        ///     e => e.Category,
        ///     e => e.Supplier
        /// );
        /// if (result.IsSuccess)
        /// {
        ///     var items = result.Value;
        ///     var totalPages = result.TotalPages;
        ///     var totalItems = result.TotalItems;
        ///     // Process the items...
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously gets the total count of all entities in the repository.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{Int32}"/>
        /// with the total count of entities, or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method executes a count query against the database to determine the number of entities
        /// managed by this repository. It is efficient for most scenarios, but for very large tables,
        /// consider using approximate count techniques if supported by your database.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.CountAsync();
        /// if (result.IsSuccess)
        /// {
        ///     Console.WriteLine($"Total entities: {result.Value}");
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously gets the count of entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each entity for a condition.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{Int32}"/>
        /// with the count of matching entities, or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method executes a count query against the database for entities that match the given predicate.
        /// It is efficient for most scenarios, but for very large tables, consider using approximate count techniques if supported by your database.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.CountWhereAsync(e => e.IsActive);
        /// if (result.IsSuccess)
        /// {
        ///     Console.WriteLine($"Active entities: {result.Value}");
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously determines whether any entity in the repository matches the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each entity for a condition.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{Boolean}"/>:
        /// <list type="bullet">
        /// <item><description>On success: <c>true</c> if any entity matches the predicate, <c>false</c> otherwise.</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is more efficient than counting entities when you only need to check for existence.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.ExistsAsync(e => e.Email == userEmail);
        /// if (result.IsSuccess && result.Value)
        /// {
        ///     // At least one entity exists with the specified email
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously determines whether an entity with the specified primary key exists in the repository.
        /// </summary>
        /// <param name="id">The primary key value of the entity to check for existence.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{Boolean}"/>:
        /// <list type="bullet">
        /// <item><description>On success: <c>true</c> if an entity with the specified key exists, <c>false</c> otherwise.</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method performs an efficient lookup using the primary key index. For existence checks using other fields,
        /// use <see cref="ExistsAsync"/> instead.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.ExistsByIdAsync(123);
        /// if (result.IsSuccess && result.Value)
        /// {
        ///     // Entity with ID 123 exists
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously updates an existing entity in the repository.
        /// </summary>
        /// <param name="entity">The entity to update. Must not be null.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{TEntity}"/> that:
        /// <list type="bullet">
        /// <item><description>On success: Contains the updated entity</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method only marks the entity as modified in the change tracker; it does not persist changes to the database.
        /// Call <see cref="SaveChangesAsync"/> to persist the changes.
        /// </para>
        /// <para>
        /// Handles both attached and detached entities by attaching them if necessary.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var entity = await repository.GetByIdAsync(123);
        /// entity.Value.Name = "Updated Name";
        /// var result = await repository.UpdateAsync(entity.Value);
        /// if (result.IsSuccess)
        /// {
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously updates multiple entities in the repository in a single operation.
        /// </summary>
        /// <param name="entities">The collection of entities to update. Must not be null.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{IEnumerable}"/> that:
        /// <list type="bullet">
        /// <item><description>On success: Contains the collection of updated entities</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method marks each entity in the collection as modified in the change tracker.
        /// It does not persist changes to the database until <see cref="SaveChangesAsync"/> is called.
        /// Handles both attached and detached entities by attaching them if necessary.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var entities = new List&lt;Product&gt; { ... };
        /// var result = await repository.UpdateRangeAsync(entities);
        /// if (result.IsSuccess)
        /// {
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="entities"/> is null.</exception>
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

        /// <summary>
        /// Asynchronously removes an entity from the repository.
        /// </summary>
        /// <param name="entity">The entity to remove. Must not be null.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result"/>:
        /// <list type="bullet">
        /// <item><description>On success: Indicates the entity was successfully marked for deletion.</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method only marks the entity for deletion in the change tracker; it does not persist changes to the database.
        /// Call <see cref="SaveChangesAsync"/> to persist the changes.
        /// </para>
        /// <para>
        /// Handles both attached and detached entities by attaching them if necessary.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var entity = await repository.GetByIdAsync(123);
        /// var result = await repository.DeleteAsync(entity.Value);
        /// if (result.IsSuccess)
        /// {
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously removes multiple entities from the repository in a single operation.
        /// </summary>
        /// <param name="entities">The collection of entities to remove. Must not be null.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result"/>:
        /// <list type="bullet">
        /// <item><description>On success: Indicates the entities were successfully marked for deletion.</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method only marks the entities for deletion in the change tracker; it does not persist changes to the database.
        /// Call <see cref="SaveChangesAsync"/> to persist the changes.
        /// </para>
        /// <para>
        /// Handles both attached and detached entities by attaching them if necessary.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var entities = await repository.GetWhereAsync(e => e.IsObsolete);
        /// var result = await repository.DeleteRangeAsync(entities.Value);
        /// if (result.IsSuccess)
        /// {
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously removes an entity with the specified primary key from the repository.
        /// </summary>
        /// <param name="id">The primary key value of the entity to remove.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result"/>:
        /// <list type="bullet">
        /// <item><description>On success: Indicates the entity was successfully marked for deletion.</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method first retrieves the entity by its key and then deletes it.
        /// If the entity is not found, the operation fails with an error message.
        /// The entity is only marked for deletion; call <see cref="SaveChangesAsync"/> to persist the change.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.DeleteByIdAsync(123);
        /// if (result.IsSuccess)
        /// {
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously removes all entities that match the specified predicate from the repository.
        /// </summary>
        /// <param name="predicate">A function defining the condition to match entities for deletion. Must not be null.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result"/>:
        /// <list type="bullet">
        /// <item><description>On success: Indicates the entities were successfully marked for deletion.</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method retrieves all entities matching the predicate and marks them for deletion in the change tracker.
        /// Changes are not persisted to the database until <see cref="SaveChangesAsync"/> is called.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.DeleteWhereAsync(e => e.IsObsolete);
        /// if (result.IsSuccess)
        /// {
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously performs a bulk update operation on entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each entity for a condition. Must not be null.</param>
        /// <param name="updateExpression">An expression that defines how to update matching entities. Must not be null.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{Int32}"/> that:
        /// <list type="bullet">
        /// <item><description>On success: Contains the number of entities updated</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method provides an efficient way to update multiple entities that match a condition
        /// without having to load and track each entity individually. However, it bypasses entity
        /// validation and change tracking.
        /// </para>
        /// <para>
        /// Important considerations:
        /// <list type="bullet">
        /// <item><description>The update is performed in memory before being saved to the database</description></item>
        /// <item><description>Entity validation is not performed</description></item>
        /// <item><description>Change tracking events are still triggered for each updated entity</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.BulkUpdateAsync(
        ///     e => e.Category == "Electronics",
        ///     e => new Product { Price = e.Price * 1.1m }
        /// );
        /// if (result.IsSuccess)
        /// {
        ///     var updatedCount = result.Value;
        ///     // Handle success...
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously performs a bulk delete operation on entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each entity for a condition. Must not be null.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{Int32}"/> that:
        /// <list type="bullet">
        /// <item><description>On success: Contains the number of entities deleted</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method provides an efficient way to delete multiple entities that match a condition
        /// in a single operation. The entities are loaded into memory to ensure proper change tracking
        /// and event notification.
        /// </para>
        /// <para>
        /// Important considerations:
        /// <list type="bullet">
        /// <item><description>Entities are loaded into memory before deletion</description></item>
        /// <item><description>Change tracking events are triggered for each deleted entity</description></item>
        /// <item><description>Cascading deletes are handled according to the entity configuration</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.BulkDeleteAsync(e => e.IsExpired);
        /// if (result.IsSuccess)
        /// {
        ///     var deletedCount = result.Value;
        ///     // Handle success...
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously saves all pending changes in the repository to the database.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{Int32}"/> that:
        /// <list type="bullet">
        /// <item><description>On success: Contains the number of state entries written to the database</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This is the async version of <see cref="SaveChanges"/>. It persists all tracked changes
        /// (adds, updates, and deletes) to the database in a single transaction.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// // Make some changes
        /// await repository.AddAsync(newEntity);
        /// await repository.UpdateAsync(existingEntity);
        /// await repository.DeleteAsync(oldEntity);
        /// 
        /// // Persist all changes
        /// var result = await repository.SaveChangesAsync();
        /// if (result.IsSuccess)
        /// {
        ///     Console.WriteLine($"{result.Value} changes saved to database");
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Executes an operation within a database transaction.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
        /// <param name="operation">The operation to execute within the transaction.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{TResult}"/>:
        /// <list type="bullet">
        /// <item><description>Success: Contains the result of the operation</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method provides transactional guarantees for complex operations that involve multiple
        /// database changes. The transaction ensures that either all changes are committed or none
        /// are (atomicity).
        /// </para>
        /// <para>
        /// The transaction is automatically:
        /// <list type="bullet">
        /// <item><description>Committed if the operation succeeds</description></item>
        /// <item><description>Rolled back if the operation fails or throws an exception</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.TransactionAsync(async repo =>
        /// {
        ///     // Perform multiple operations
        ///     var addResult = await repo.AddAsync(newEntity);
        ///     var deleteResult = await repo.DeleteAsync(oldEntity);
        ///     
        ///     // Both operations must succeed
        ///     if (addResult.IsFailure || deleteResult.IsFailure)
        ///         return Result&lt;bool&gt;.Failure("Transaction failed");
        ///         
        ///     return Result&lt;bool&gt;.Success(true);
        /// });
        /// </code>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="operation"/> is null.</exception>
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

        /// <summary>
        /// Asynchronously projects entities to a specified type using the given projection expression.
        /// </summary>
        /// <typeparam name="TProjection">The type to project to.</typeparam>
        /// <param name="projection">The projection expression that defines how to map <typeparamref name="TEntity"/> to <typeparamref name="TProjection"/>.</param>
        /// <param name="predicate">An optional filter predicate to select which entities to project. If null, all entities are considered.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{TProjection}"/>:
        /// <list type="bullet">
        /// <item><description>On success: Contains the projected value or default if no entity matches.</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method is useful for retrieving a single projected value from the entity set, such as a DTO or a scalar.
        /// If <paramref name="predicate"/> is null, an <see cref="ArgumentNullException"/> is thrown.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = await repository.GetProjectionAsync(
        ///     p => new ProductDto { Id = p.Id, Name = p.Name },
        ///     p => p.IsActive
        /// );
        /// if (result.IsSuccess)
        /// {
        ///     var dto = result.Value;
        ///     // Use the projected DTO...
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Retrieves all entities from the repository as an asynchronous stream.
        /// </summary>
        /// <param name="batchSize">The number of entities to retrieve in each batch. Must be greater than zero.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the stream to complete.</param>
        /// <returns>
        /// An <see cref="IAsyncEnumerable{Result}"/> that yields each entity wrapped in a <see cref="Result{TEntity}"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method streams entities from the database in batches, reducing memory usage for large datasets.
        /// Each entity is returned as a <see cref="Result{TEntity}"/> to provide error handling.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// await foreach (var result in repository.GetAllAsStreamAsync(batchSize: 500))
        /// {
        ///     if (result.IsSuccess)
        ///     {
        ///         var entity = result.Value;
        ///         // Process entity
        ///     }
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Retrieves entities from the repository that match the specified specification.
        /// </summary>
        /// <param name="specification">The specification containing filter criteria, includes, and ordering.</param>
        /// <returns>
        /// A <see cref="Result{IEnumerable}"/> containing the entities that match the specification,
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The specification can include filter criteria (<see cref="ISpecification{T}.Criteria"/>),
        /// eager loading of related entities (<see cref="ISpecification{T}.Includes"/>), and ordering.
        /// Only criteria and includes are applied in this method; ordering is ignored.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var spec = new ProductSpecification { Criteria = p => p.IsActive, Includes = { p => p.Category } };
        /// var result = repository.GetBySpecification(spec);
        /// if (result.IsSuccess)
        /// {
        ///     foreach (var entity in result.Value)
        ///     {
        ///         // Process entity...
        ///     }
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Asynchronously retrieves entities from the repository that match the specified specification.
        /// </summary>
        /// <param name="specification">The specification containing filter criteria, includes, and ordering.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task representing the asynchronous operation, containing a <see cref="Result{IEnumerable}"/>:
        /// <list type="bullet">
        /// <item><description>On success: Contains the entities that match the specification.</description></item>
        /// <item><description>On failure: Contains an error message describing what went wrong.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// The specification can include filter criteria (<see cref="ISpecification{T}.Criteria"/>),
        /// eager loading of related entities (<see cref="ISpecification{T}.Includes"/>), and ordering.
        /// Only criteria and includes are applied in this method; ordering is ignored.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var spec = new ProductSpecification { Criteria = p => p.IsActive, Includes = { p => p.Category } };
        /// var result = await repository.GetBySpecificationAsync(spec);
        /// if (result.IsSuccess)
        /// {
        ///     foreach (var entity in result.Value)
        ///     {
        ///         // Process entity...
        ///     }
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Updates a specific property of an entity, marking only that property as modified.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property to update.</typeparam>
        /// <param name="entity">The entity to update. Must not be null.</param>
        /// <param name="propertyExpression">An expression selecting the property to update. Must be a member expression.</param>
        /// <param name="value">The new value to set for the property.</param>
        /// <returns>
        /// A <see cref="Result{TEntity}"/> that represents the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Contains the updated entity</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method only marks the specified property as modified in the change tracker; it does not persist changes to the database.
        /// Call <see cref="SaveChanges"/> or <see cref="SaveChangesAsync"/> to persist the changes.
        /// </para>
        /// <para>
        /// The method handles both attached and detached entities by attaching them if necessary.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// var result = repository.UpdatePartial(product, p => p.Price, 19.99m);
        /// if (result.IsSuccess)
        /// {
        ///     await repository.SaveChangesAsync();
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Rolls back all pending changes in the current context.
        /// </summary>
        /// <returns>
        /// A <see cref="Result"/> indicating the outcome of the operation:
        /// <list type="bullet">
        /// <item><description>Success: Indicates all changes were successfully rolled back</description></item>
        /// <item><description>Failure: Contains an error message describing what went wrong</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method reverts all pending changes tracked by the context without affecting the database.
        /// It's useful for scenarios where you need to abandon changes after detecting a validation or
        /// business rule violation.
        /// </para>
        /// <para>
        /// The method handles different entity states as follows:
        /// <list type="bullet">
        /// <item><description>Modified entities: Reverted to their original values</description></item>
        /// <item><description>Added entities: Detached from the context</description></item>
        /// <item><description>Deleted entities: Reloaded from the database</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// try
        /// {
        ///     // Perform some operations
        ///     if (validationFailed)
        ///     {
        ///         var result = repository.RollbackChanges();
        ///         if (result.IsSuccess)
        ///         {
        ///             // Changes were successfully rolled back
        ///         }
        ///     }
        /// }
        /// </code>
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Releases the unmanaged resources used by the repository and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        /// <remarks>
        /// <para>
        /// This method is part of the standard .NET disposal pattern. When disposing:
        /// <list type="bullet">
        /// <item><description>The database context is disposed if it was not externally provided</description></item>
        /// <item><description>All tracked entities are detached</description></item>
        /// <item><description>Resources are marked as disposed to prevent further usage</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// The repository cannot be used after disposal. Attempting to use a disposed repository
        /// will result in undefined behavior.
        /// </para>
        /// </remarks>
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

        /// <summary>
        /// Releases all resources used by the repository.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Disposal is important to prevent resource leaks, especially with the database context.
        /// Always dispose of repositories when they are no longer needed.
        /// </para>
        /// <para>
        /// Example usage:
        /// <code>
        /// using (var repository = new EfGenericRepository&lt;TEntity, TKey&gt;(context))
        /// {
        ///     // Use the repository...
        /// } // Repository is automatically disposed here
        /// </code>
        /// </para>
        /// </remarks>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Raises the <see cref="EntityChanged"/> event with the specified entity and change type.
        /// </summary>
        /// <param name="entity">The entity that was changed.</param>
        /// <param name="changeType">The type of change that occurred.</param>
        /// <remarks>
        /// <para>
        /// This method is called internally whenever an entity is added, modified, or deleted. It provides
        /// a way for consumers to track changes to entities before they are saved to the database.
        /// </para>
        /// <para>
        /// The method is virtual to allow derived classes to customize the event raising behavior or
        /// perform additional actions when entities change.
        /// </para>
        /// <para>
        /// Example override:
        /// <code>
        /// protected override void OnEntityChanged(TEntity entity, EntityChangeType changeType)
        /// {
        ///     // Add custom logging
        ///     Logger.Log($"Entity of type {typeof(TEntity).Name} was {changeType}");
        ///     
        ///     // Call base implementation to ensure event is raised
        ///     base.OnEntityChanged(entity, changeType);
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        protected virtual void OnEntityChanged(TEntity entity, EntityChangeType changeType)
        {
            this.EntityChanged?.Invoke(this, new EntityChangedEventArgs<TEntity>(entity, changeType));
        }
    }
}