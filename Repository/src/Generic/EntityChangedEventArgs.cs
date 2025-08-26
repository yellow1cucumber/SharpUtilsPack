namespace SharpUtils.Repository.Generic
{

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