using System.Linq.Expressions;
using SharpUtils.Results;

namespace SharpUtils.Repository.Generic
{
    /// <summary>
    /// Defines a generic repository interface that provides comprehensive data access operations for entities.
    /// This interface follows the Repository pattern and provides both synchronous and asynchronous methods
    /// with a functional approach to error handling using the Result pattern.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity that this repository manages. Must be a reference type.</typeparam>
    /// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
    public interface IGenericRepository<TEntity, TKey> : IDisposable
        where TEntity : class
        where TKey : IEquatable<TKey>
    {
        #region SYNC

        #region Create Operations

        /// <summary>
        /// Adds a new entity to the repository.
        /// </summary>
        /// <param name="entity">The entity to add. Cannot be null.</param>
        /// <returns>
        /// A <see cref="Result{TEntity}"/> containing the added entity if successful,
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This operation adds the entity to the change tracker but does not persist changes to the database.
        /// Call <see cref="SaveChanges"/> to persist the changes.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
        Result<TEntity> Add(TEntity entity);

        /// <summary>
        /// Adds multiple entities to the repository in a single operation.
        /// </summary>
        /// <param name="entities">The collection of entities to add. Cannot be null or contain null elements.</param>
        /// <returns>
        /// A <see cref="Result{IEnumerable}"/> containing the added entities if successful,
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This operation is more efficient than calling <see cref="Add"/> multiple times
        /// as it processes all entities in a single batch operation.
        /// </remarks>
        Result<IEnumerable<TEntity>> AddRange(IEnumerable<TEntity> entities);

        #endregion

        #region Read Operations

        #region Single Entity Retrieval

        /// <summary>
        /// Retrieves an entity by its primary key.
        /// </summary>
        /// <param name="id">The primary key value to search for.</param>
        /// <returns>
        /// A <see cref="Result{TEntity}"/> containing the found entity or null if not found,
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This method assumes the entity has a single primary key property.
        /// For composite keys, use <see cref="GetFirstOrDefault"/> with an appropriate predicate.
        /// </remarks>
        Result<TEntity?> GetById(TKey id);

        /// <summary>
        /// Retrieves the first entity that matches the specified predicate, or null if no match is found.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Result{TEntity}"/> containing the first matching entity or null if no match is found,
        /// or an error message if the operation fails.
        /// </returns>
        /// <example>
        /// <code>
        /// var result = repository.GetFirstOrDefault(p => p.Name == "ProductName");
        /// </code>
        /// </example>
        Result<TEntity?> GetFirstOrDefault(Expression<Func<TEntity, bool>> predicate);

        #endregion

        #region Multiple Entity Retrieval

        /// <summary>
        /// Retrieves all entities from the repository.
        /// </summary>
        /// <returns>
        /// A <see cref="Result{IEnumerable}"/> containing all entities,
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// <para>Use with caution on large datasets as it loads all entities into memory.</para>
        /// <para>Consider using pagination methods like <see cref="GetPaged"/> for large datasets.</para>
        /// </remarks>
        Result<IEnumerable<TEntity>> GetAll();

        /// <summary>
        /// Retrieves all entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Result{IEnumerable}"/> containing all matching entities,
        /// or an error message if the operation fails.
        /// </returns>
        /// <example>
        /// <code>
        /// var result = repository.GetWhere(p => p.IsActive &amp;&amp; p.Price &gt; 100);
        /// </code>
        /// </example>
        Result<IEnumerable<TEntity>> GetWhere(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Retrieves all entities with the specified related entities eagerly loaded.
        /// </summary>
        /// <param name="includes">Navigation properties to include in the query.</param>
        /// <returns>
        /// A <see cref="Result{IEnumerable}"/> containing all entities with included properties,
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This method enables eager loading of related entities to avoid N+1 query problems.
        /// </remarks>
        /// <example>
        /// <code>
        /// var result = repository.GetWithInclude(p => p.Category, p => p.Reviews);
        /// </code>
        /// </example>
        Result<IEnumerable<TEntity>> GetWithInclude(params Expression<Func<TEntity, object>>[] includes);

        /// <summary>
        /// Retrieves entities that match the specified predicate with related entities eagerly loaded.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="includes">Navigation properties to include in the query.</param>
        /// <returns>
        /// A <see cref="Result{IEnumerable}"/> containing matching entities with included properties,
        /// or an error message if the operation fails.
        /// </returns>
        /// <example>
        /// <code>
        /// var result = repository.GetWhereWithInclude(
        ///     p => p.IsActive,
        ///     p => p.Category,
        ///     p => p.Reviews);
        /// </code>
        /// </example>
        Result<IEnumerable<TEntity>> GetWhereWithInclude(
            Expression<Func<TEntity, bool>> predicate,
            params Expression<Func<TEntity, object>>[] includes);

        #endregion

        #region Pagination

        /// <summary>
        /// Retrieves a page of entities from the repository.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <returns>
        /// A <see cref="PaginatedResult{TEntity}"/> containing the page of entities and pagination metadata,
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// Page numbers are 1-based. Requesting page 0 or negative numbers may result in an error.
        /// </remarks>
        PaginatedResult<TEntity> GetPaged(int pageNumber, int pageSize);

        /// <summary>
        /// Retrieves a page of entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <returns>
        /// A <see cref="PaginatedResult{TEntity}"/> containing the page of matching entities and pagination metadata,
        /// or an error message if the operation fails.
        /// </returns>
        PaginatedResult<TEntity> GetPagedWhere(
            Expression<Func<TEntity, bool>> predicate,
            int pageNumber,
            int pageSize);

        /// <summary>
        /// Retrieves a page of entities ordered by the specified key.
        /// </summary>
        /// <param name="orderBy">The expression that specifies the key to order by.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <param name="ascending">If true, orders in ascending order; otherwise, descending order.</param>
        /// <returns>
        /// A <see cref="PaginatedResult{TEntity}"/> containing the ordered page of entities and pagination metadata,
        /// or an error message if the operation fails.
        /// </returns>
        PaginatedResult<TEntity> GetPagedOrdered(
            Expression<Func<TEntity, TKey>> orderBy,
            int pageNumber,
            int pageSize,
            bool ascending = true);

        /// <summary>
        /// Retrieves a page of entities that match the specified predicate with related entities eagerly loaded.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <param name="includes">Navigation properties to include in the query.</param>
        /// <returns>
        /// A <see cref="PaginatedResult{TEntity}"/> containing the page of matching entities with included properties and pagination metadata,
        /// or an error message if the operation fails.
        /// </returns>
        PaginatedResult<TEntity> GetPagedWhereWithInclude(
            Expression<Func<TEntity, bool>> predicate,
            int pageNumber,
            int pageSize,
            params Expression<Func<TEntity, object>>[] includes);

        /// <summary>
        /// Retrieves a page of entities ordered by the specified key with related entities eagerly loaded.
        /// </summary>
        /// <param name="orderBy">The expression that specifies the key to order by.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <param name="ascending">If true, orders in ascending order; otherwise, descending order.</param>
        /// <param name="includes">Navigation properties to include in the query.</param>
        /// <returns>
        /// A <see cref="PaginatedResult{TEntity}"/> containing the ordered page of entities with included properties and pagination metadata,
        /// or an error message if the operation fails.
        /// </returns>
        PaginatedResult<TEntity> GetPagedOrderedWithInclude(
            Expression<Func<TEntity, TKey>> orderBy,
            int pageNumber,
            int pageSize,
            bool ascending = true,
            params Expression<Func<TEntity, object>>[] includes);

        #endregion

        #region Count Operations

        /// <summary>
        /// Gets the total count of all entities in the repository.
        /// </summary>
        /// <returns>
        /// A <see cref="Result{Int32}"/> containing the total count of entities,
        /// or an error message if the operation fails.
        /// </returns>
        Result<int> Count();

        /// <summary>
        /// Gets the count of entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Result{Int32}"/> containing the count of matching entities,
        /// or an error message if the operation fails.
        /// </returns>
        /// <example>
        /// <code>
        /// var result = repository.CountWhere(p => p.IsActive);
        /// </code>
        /// </example>
        Result<int> CountWhere(Expression<Func<TEntity, bool>> predicate);

        #endregion

        #region Existence Checks

        /// <summary>
        /// Determines whether any entity matches the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Result{Boolean}"/> containing true if any entity matches the predicate, false otherwise,
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This method is more efficient than using <see cref="CountWhere"/> when you only need to check for existence.
        /// </remarks>
        /// <example>
        /// <code>
        /// var result = repository.Exists(p => p.Email == userEmail);
        /// </code>
        /// </example>
        Result<bool> Exists(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Determines whether an entity with the specified primary key exists.
        /// </summary>
        /// <param name="id">The primary key value to check for.</param>
        /// <returns>
        /// A <see cref="Result{Boolean}"/> containing true if an entity with the specified key exists, false otherwise,
        /// or an error message if the operation fails.
        /// </returns>
        Result<bool> ExistsById(TKey id);

        #endregion

        #endregion

        #region Update Operations

        /// <summary>
        /// Updates an existing entity in the repository.
        /// </summary>
        /// <param name="entity">The entity to update. Cannot be null.</param>
        /// <returns>
        /// A <see cref="Result{TEntity}"/> containing the updated entity if successful,
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This operation marks the entity as modified in the change tracker but does not persist changes to the database.
        /// Call <see cref="SaveChanges"/> to persist the changes.
        /// </remarks>
        Result<TEntity> Update(TEntity entity);

        /// <summary>
        /// Updates multiple entities in the repository in a single operation.
        /// </summary>
        /// <param name="entities">The collection of entities to update. Cannot be null or contain null elements.</param>
        /// <returns>
        /// A <see cref="Result{IEnumerable}"/> containing the updated entities if successful,
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This operation is more efficient than calling <see cref="Update"/> multiple times
        /// as it processes all entities in a single batch operation.
        /// </remarks>
        Result<IEnumerable<TEntity>> UpdateRange(IEnumerable<TEntity> entities);

        #endregion

        #region Delete Operations

        /// <summary>
        /// Removes an entity from the repository.
        /// </summary>
        /// <param name="entity">The entity to remove. Cannot be null.</param>
        /// <returns>
        /// A <see cref="Result"/> indicating success or failure of the operation.
        /// </returns>
        /// <remarks>
        /// This operation marks the entity for deletion in the change tracker but does not persist changes to the database.
        /// Call <see cref="SaveChanges"/> to persist the changes.
        /// </remarks>
        Result Delete(TEntity entity);

        /// <summary>
        /// Removes multiple entities from the repository in a single operation.
        /// </summary>
        /// <param name="entities">The collection of entities to remove. Cannot be null or contain null elements.</param>
        /// <returns>
        /// A <see cref="Result"/> indicating success or failure of the operation.
        /// </returns>
        /// <remarks>
        /// This operation is more efficient than calling <see cref="Delete"/> multiple times
        /// as it processes all entities in a single batch operation.
        /// </remarks>
        Result DeleteRange(IEnumerable<TEntity> entities);

        /// <summary>
        /// Removes an entity with the specified primary key from the repository.
        /// </summary>
        /// <param name="id">The primary key value of the entity to remove.</param>
        /// <returns>
        /// A <see cref="Result"/> indicating success or failure of the operation.
        /// </returns>
        /// <remarks>
        /// This method first retrieves the entity by its key and then deletes it.
        /// If the entity is not found, the operation may still succeed (idempotent delete).
        /// </remarks>
        Result DeleteById(TKey id);

        /// <summary>
        /// Removes all entities that match the specified predicate from the repository.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Result"/> indicating success or failure of the operation.
        /// </returns>
        /// <remarks>
        /// This operation retrieves all matching entities and then deletes them.
        /// For large datasets, consider using <see cref="BulkDelete"/> for better performance.
        /// </remarks>
        /// <example>
        /// <code>
        /// var result = repository.DeleteWhere(p => p.IsExpired);
        /// </code>
        /// </example>
        Result DeleteWhere(Expression<Func<TEntity, bool>> predicate);

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Performs a bulk update operation on entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="updateExpression">An expression that defines how to update the matching entities.</param>
        /// <returns>
        /// A <see cref="Result{Int32}"/> containing the number of entities updated,
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This operation performs the update directly in the database without loading entities into memory,
        /// making it very efficient for large-scale updates.
        /// </remarks>
        /// <example>
        /// <code>
        /// var result = repository.BulkUpdate(
        ///     p => p.IsActive,
        ///     p => new Product { LastUpdated = DateTime.UtcNow });
        /// </code>
        /// </example>
        Result<int> BulkUpdate(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TEntity>> updateExpression);

        /// <summary>
        /// Performs a bulk delete operation on entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>
        /// A <see cref="Result{Int32}"/> containing the number of entities deleted,
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This operation performs the deletion directly in the database without loading entities into memory,
        /// making it very efficient for large-scale deletions.
        /// </remarks>
        /// <example>
        /// <code>
        /// var result = repository.BulkDelete(p => p.IsExpired);
        /// </code>
        /// </example>
        Result<int> BulkDelete(Expression<Func<TEntity, bool>> predicate);

        #endregion

        #region Persistence

        /// <summary>
        /// Saves all pending changes to the underlying data store.
        /// </summary>
        /// <returns>
        /// A <see cref="Result{Int32}"/> containing the number of entities affected,
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This method commits all changes made through Add, Update, Delete operations to the database.
        /// It should be called after making changes to persist them permanently.
        /// </remarks>
        Result<int> SaveChanges();

        #endregion

        #endregion

        #region ASYNC

        #region Create Operations

        /// <summary>
        /// Asynchronously adds a new entity to the repository.
        /// </summary>
        /// <param name="entity">The entity to add. Cannot be null.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{TEntity}"/>
        /// with the added entity if successful, or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This operation adds the entity to the change tracker but does not persist changes to the database.
        /// Call <see cref="SaveChangesAsync"/> to persist the changes.
        /// </remarks>
        Task<Result<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously adds multiple entities to the repository in a single operation.
        /// </summary>
        /// <param name="entities">The collection of entities to add. Cannot be null or contain null elements.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{IEnumerable}"/>
        /// with the added entities if successful, or an error message if the operation fails.
        /// </returns>
        Task<Result<IEnumerable<TEntity>>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        #endregion

        #region Read Operations

        #region Single Entity Retrieval

        /// <summary>
        /// Asynchronously retrieves an entity by its primary key.
        /// </summary>
        /// <param name="id">The primary key value to search for.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{TEntity}"/>
        /// with the found entity or null if not found, or an error message if the operation fails.
        /// </returns>
        Task<Result<TEntity?>> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves the first entity that matches the specified predicate, or null if no match is found.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{TEntity}"/>
        /// with the first matching entity or null if no match is found, or an error message if the operation fails.
        /// </returns>
        Task<Result<TEntity?>> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        #endregion

        #region Multiple Entity Retrieval

        /// <summary>
        /// Asynchronously retrieves all entities from the repository.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{IEnumerable}"/>
        /// with all entities, or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// Use with caution on large datasets as it loads all entities into memory.
        /// Consider using pagination methods like <see cref="GetPagedAsync"/> for large datasets.
        /// </remarks>
        Task<Result<IEnumerable<TEntity>>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves all entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{IEnumerable}"/>
        /// with all matching entities, or an error message if the operation fails.
        /// </returns>
        Task<Result<IEnumerable<TEntity>>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves all entities with the specified related entities eagerly loaded.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <param name="includes">Navigation properties to include in the query.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{IEnumerable}"/>
        /// with all entities with included properties, or an error message if the operation fails.
        /// </returns>
        Task<Result<IEnumerable<TEntity>>> GetWithIncludeAsync(CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes);

        /// <summary>
        /// Asynchronously retrieves entities that match the specified predicate with related entities eagerly loaded.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <param name="includes">Navigation properties to include in the query.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{IEnumerable}"/>
        /// with matching entities with included properties, or an error message if the operation fails.
        /// </returns>
        Task<Result<IEnumerable<TEntity>>> GetWhereWithIncludeAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object>>[] includes);

        #endregion

        #region Pagination

        /// <summary>
        /// Asynchronously retrieves a page of entities from the repository.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="PaginatedResult{TEntity}"/>
        /// with the page of entities and pagination metadata, or an error message if the operation fails.
        /// </returns>
        Task<PaginatedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves a page of entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="PaginatedResult{TEntity}"/>
        /// with the page of matching entities and pagination metadata, or an error message if the operation fails.
        /// </returns>
        Task<PaginatedResult<TEntity>> GetPagedWhereAsync(
            Expression<Func<TEntity, bool>> predicate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves a page of entities ordered by the specified key.
        /// </summary>
        /// <param name="orderBy">The expression that specifies the key to order by.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <param name="ascending">If true, orders in ascending order; otherwise, descending order.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="PaginatedResult{TEntity}"/>
        /// with the ordered page of entities and pagination metadata, or an error message if the operation fails.
        /// </returns>
        Task<PaginatedResult<TEntity>> GetPagedOrderedAsync(
            Expression<Func<TEntity, TKey>> orderBy,
            int pageNumber,
            int pageSize,
            bool ascending = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously retrieves a page of entities that match the specified predicate with related entities eagerly loaded.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <param name="includes">Navigation properties to include in the query.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="PaginatedResult{TEntity}"/>
        /// with the page of matching entities with included properties and pagination metadata, or an error message if the operation fails.
        /// </returns>
        Task<PaginatedResult<TEntity>> GetPagedWhereWithIncludeAsync(
            Expression<Func<TEntity, bool>> predicate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object>>[] includes);

        /// <summary>
        /// Asynchronously retrieves a page of entities ordered by the specified key with related entities eagerly loaded.
        /// </summary>
        /// <param name="orderBy">The expression that specifies the key to order by.</param>
        /// <param name="pageNumber">The page number to retrieve (1-based).</param>
        /// <param name="pageSize">The number of entities per page.</param>
        /// <param name="ascending">If true, orders in ascending order; otherwise, descending order.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <param name="includes">Navigation properties to include in the query.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="PaginatedResult{TEntity}"/>
        /// with the ordered page of entities with included properties and pagination metadata, or an error message if the operation fails.
        /// </returns>
        Task<PaginatedResult<TEntity>> GetPagedOrderedWithIncludeAsync(
            Expression<Func<TEntity, TKey>> orderBy,
            int pageNumber,
            int pageSize,
            bool ascending = true,
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object>>[] includes);

        #endregion

        #region Count Operations

        /// <summary>
        /// Asynchronously gets the total count of all entities in the repository.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{Int32}"/>
        /// with the total count of entities, or an error message if the operation fails.
        /// </returns>
        Task<Result<int>> CountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets the count of entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{Int32}"/>
        /// with the count of matching entities, or an error message if the operation fails.
        /// </returns>
        Task<Result<int>> CountWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        #endregion

        #region Existence Checks

        /// <summary>
        /// Asynchronously determines whether any entity matches the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{Boolean}"/>
        /// with true if any entity matches the predicate, false otherwise, or an error message if the operation fails.
        /// </returns>
        Task<Result<bool>> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously determines whether an entity with the specified primary key exists.
        /// </summary>
        /// <param name="id">The primary key value to check for.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{Boolean}"/>
        /// with true if an entity with the specified key exists, false otherwise, or an error message if the operation fails.
        /// </returns>
        Task<Result<bool>> ExistsByIdAsync(TKey id, CancellationToken cancellationToken = default);

        #endregion

        #endregion

        #region Update Operations

        /// <summary>
        /// Asynchronously updates an existing entity in the repository.
        /// </summary>
        /// <param name="entity">The entity to update. Cannot be null.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{TEntity}"/>
        /// with the updated entity if successful, or an error message if the operation fails.
        /// </returns>
        Task<Result<TEntity>> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously updates multiple entities in the repository in a single operation.
        /// </summary>
        /// <param name="entities">The collection of entities to update. Cannot be null or contain null elements.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{IEnumerable}"/>
        /// with the updated entities if successful, or an error message if the operation fails.
        /// </returns>
        Task<Result<IEnumerable<TEntity>>> UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        #endregion

        #region Delete Operations

        /// <summary>
        /// Asynchronously removes an entity from the repository.
        /// </summary>
        /// <param name="entity">The entity to remove. Cannot be null.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result"/>
        /// indicating success or failure of the operation.
        /// </returns>
        Task<Result> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously removes multiple entities from the repository in a single operation.
        /// </summary>
        /// <param name="entities">The collection of entities to remove. Cannot be null or contain null elements.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result"/>
        /// indicating success or failure of the operation.
        /// </returns>
        Task<Result> DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously removes an entity with the specified primary key from the repository.
        /// </summary>
        /// <param name="id">The primary key value of the entity to remove.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result"/>
        /// indicating success or failure of the operation.
        /// </returns>
        Task<Result> DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously removes all entities that match the specified predicate from the repository.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result"/>
        /// indicating success or failure of the operation.
        /// </returns>
        Task<Result> DeleteWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Asynchronously performs a bulk update operation on entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="updateExpression">An expression that defines how to update the matching entities.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{Int32}"/>
        /// with the number of entities updated, or an error message if the operation fails.
        /// </returns>
        Task<Result<int>> BulkUpdateAsync(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TEntity>> updateExpression, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously performs a bulk delete operation on entities that match the specified predicate.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{Int32}"/>
        /// with the number of entities deleted, or an error message if the operation fails.
        /// </returns>
        Task<Result<int>> BulkDeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        #endregion

        #region Persistence

        /// <summary>
        /// Asynchronously saves all pending changes to the underlying data store.
        /// </summary>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result{Int32}"/>
        /// with the number of entities affected, or an error message if the operation fails.
        /// </returns>
        Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken = default);

        #endregion

        #region Events
        /// <summary>
        /// Occurs when an entity is changed in the repository.
        /// </summary>
        event EventHandler<EntityChangedEventArgs<TEntity>> EntityChanged;
        #endregion

        #region Query Interface
        /// <summary>
        /// Gets an IQueryable interface for the entity type.
        /// </summary>
        /// <remarks>
        /// Use with caution as this exposes the underlying query capabilities.
        /// Prefer using the provided repository methods when possible.
        /// </remarks>
        IQueryable<TEntity> Query { get; }
        #endregion

        #region Validation and Safety
        /// <summary>
        /// Rolls back all changes made in the current transaction.
        /// </summary>
        /// <returns>A Result indicating whether the rollback succeeded.</returns>
        Result RollbackChanges();
        #endregion

        #region Transactions
        /// <summary>
        /// Executes the specified operation within a transaction.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Result containing the operation result.</returns>
        Task<Result<TResult>> TransactionAsync<TResult>(
            Func<IGenericRepository<TEntity, TKey>, Task<Result<TResult>>> operation,
            CancellationToken cancellationToken = default);
        #endregion

        #region Projections and Streaming
        /// <summary>
        /// Projects entities to a different type using the specified projection.
        /// </summary>
        /// <typeparam name="TProjection">The type to project to.</typeparam>
        /// <param name="projection">The projection expression.</param>
        /// <param name="predicate">Optional filter predicate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Result containing the projected entities.</returns>
        Task<Result<TProjection>> GetProjectionAsync<TProjection>(
            Expression<Func<TEntity, TProjection>> projection,
            Expression<Func<TEntity, bool>>? predicate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves entities as an async stream for efficient memory usage.
        /// </summary>
        /// <param name="batchSize">The size of each batch.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of Results containing the entities.</returns>
        IAsyncEnumerable<Result<TEntity>> GetAllAsStreamAsync(
            int batchSize = 1000,
            CancellationToken cancellationToken = default);
        #endregion

        #region Specifications
        /// <summary>
        /// Retrieves entities that match the specified specification.
        /// </summary>
        /// <param name="specification">The specification to apply.</param>
        /// <returns>A Result containing the matching entities.</returns>
        Result<IEnumerable<TEntity>> GetBySpecification(ISpecification<TEntity> specification);

        /// <summary>
        /// Asynchronously retrieves entities that match the specified specification.
        /// </summary>
        /// <param name="specification">The specification to apply.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Result containing the matching entities.</returns>
        Task<Result<IEnumerable<TEntity>>> GetBySpecificationAsync(
            ISpecification<TEntity> specification,
            CancellationToken cancellationToken = default);
        #endregion

        #region Partial Updates
        /// <summary>
        /// Updates a specific property of an entity.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="entity">The entity to update.</param>
        /// <param name="propertyExpression">The property selector.</param>
        /// <param name="value">The new value.</param>
        /// <returns>A Result containing the updated entity.</returns>
        Result<TEntity> UpdatePartial<TProperty>(
            TEntity entity,
            Expression<Func<TEntity, TProperty>> propertyExpression,
            TProperty value);
        #endregion

        #region Advanced Pagination
        /// <summary>
        /// Retrieves a page of entities using cursor-based pagination.
        /// </summary>
        /// <param name="cursor">The cursor value.</param>
        /// <param name="pageSize">The size of the page.</param>
        /// <param name="orderBy">The ordering expression.</param>
        /// <param name="ascending">Whether to order in ascending order.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A PaginatedResult containing the page of entities.</returns>
        Task<PaginatedResult<TEntity>> GetPagedByCursorAsync(
            string cursor,
            int pageSize,
            Expression<Func<TEntity, object>> orderBy,
            bool ascending = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a cached page of entities.
        /// </summary>
        /// <param name="pageNumber">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cacheExpiration">How long to cache the results.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A PaginatedResult containing the page of entities.</returns>
        Task<PaginatedResult<TEntity>> GetPagedCachedAsync(
            int pageNumber,
            int pageSize,
            TimeSpan cacheExpiration,
            CancellationToken cancellationToken = default);
        #endregion

        #region Performance Monitoring
        /// <summary>
        /// Retrieves metrics about repository operations.
        /// </summary>
        /// <param name="start">The start of the period.</param>
        /// <param name="end">The end of the period.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Result containing the repository metrics.</returns>
        Task<Result<RepositoryMetrics>> GetMetricsAsync(
            DateTime start,
            DateTime end,
            CancellationToken cancellationToken = default);
        #endregion

        #endregion
    }
}