using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Repository.Generic
{
    /// <summary>
    /// Encapsulates query specifications for filtering, including, and ordering entities.
    /// </summary>
    /// <typeparam name="T">The type of entity this specification applies to.</typeparam>
    public interface ISpecification<T>
    {
        /// <summary>
        /// Gets the filter criteria for the specification.
        /// </summary>
        Expression<Func<T, bool>>? Criteria { get; }

        /// <summary>
        /// Gets the list of include expressions for eager loading related data.
        /// </summary>
        IList<Expression<Func<T, object>>> Includes { get; }

        /// <summary>
        /// Gets the expression for ordering the results.
        /// </summary>
        Expression<Func<T, object>>? OrderBy { get; }

        /// <summary>
        /// Gets a value indicating whether to order in ascending or descending order.
        /// </summary>
        bool OrderByDescending { get; }
    }
}