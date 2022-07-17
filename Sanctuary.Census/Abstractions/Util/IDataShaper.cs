using System.Collections.Generic;
using System.Dynamic;

namespace Sanctuary.Census.Abstractions.Util;

/// <summary>
/// Represents an interface for dynamically shaping an object
/// given a list of named properties to retrieve.
/// </summary>
public interface IDataShaper
{
    /// <summary>
    /// Dynamically gets a value from the given <paramref name="entity"/>.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="propertyName">The name of the property to get the value of.</param>
    /// <returns>The value of the property.</returns>
    object? GetValue(object entity, string propertyName);

    /// <summary>
    /// Dynamically shapes an object by taking only the
    /// given properties from the <paramref name="entity"/>.
    /// </summary>
    /// <param name="entity">The entity to shape.</param>
    /// <param name="propertyNames">The properties to include in the result.</param>
    /// <returns>The shaped object.</returns>
    ExpandoObject ShapeData(object entity, IEnumerable<string> propertyNames);

    /// <summary>
    /// Dynamically shapes an object by excluding all
    /// the given properties from the <paramref name="entity"/>.
    /// </summary>
    /// <param name="entity">The entity to shape.</param>
    /// <param name="propertyNames">The properties to exclude from the result.</param>
    /// <returns>The shaped object.</returns>
    ExpandoObject ShapeDataInverted(object entity, IEnumerable<string> propertyNames);
}
