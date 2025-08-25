using System;

namespace Repository.Generic
{
    /// <summary>
    /// Provides metadata about repository operations and their performance.
    /// </summary>
    public class RepositoryMetrics
    {
        /// <summary>
        /// Gets or sets the total number of queries executed.
        /// </summary>
        public int QueryCount { get; set; }

        /// <summary>
        /// Gets or sets the total execution time of all queries.
        /// </summary>
        public TimeSpan TotalQueryTime { get; set; }

        /// <summary>
        /// Gets or sets the number of cache hits.
        /// </summary>
        public int CacheHits { get; set; }

        /// <summary>
        /// Gets or sets the number of cache misses.
        /// </summary>
        public int CacheMisses { get; set; }
    }

    /// <summary>
    /// Represents the details of a change to an entity.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public class EntityChangedEventArgs<TEntity> : EventArgs
    {
        /// <summary>
        /// Gets the entity that was changed.
        /// </summary>
        public TEntity Entity { get; }

        /// <summary>
        /// Gets the type of change that occurred.
        /// </summary>
        public EntityChangeType ChangeType { get; }

        /// <summary>
        /// Gets the timestamp when the change occurred.
        /// </summary>
        public DateTime Timestamp { get; }

        public EntityChangedEventArgs(TEntity entity, EntityChangeType changeType)
        {
            Entity = entity;
            ChangeType = changeType;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Defines the types of changes that can occur to an entity.
    /// </summary>
    public enum EntityChangeType
    {
        /// <summary>
        /// The entity was added.
        /// </summary>
        Added,

        /// <summary>
        /// The entity was modified.
        /// </summary>
        Modified,

        /// <summary>
        /// The entity was deleted.
        /// </summary>
        Deleted
    }
}