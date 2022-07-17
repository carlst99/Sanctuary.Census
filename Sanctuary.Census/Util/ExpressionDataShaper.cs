using Sanctuary.Census.Abstractions.Util;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Sanctuary.Census.Util;

/// <inheritdoc />
public class ExpressionDataShaper<T> : IDataShaper<T>
    where T : class
{
    private readonly Type _dataType;
    private readonly Dictionary<string, Func<object, object?>> _propertyAccessors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionDataShaper{T}"/> class.
    /// </summary>
    public ExpressionDataShaper(JsonNamingPolicy nameConverter)
    {
        _dataType = typeof(T);
        _propertyAccessors = new Dictionary<string, Func<object, object?>>();

        BuildPropertyAccessors(nameConverter);
    }

    /// <inheritdoc />
    public object? GetValue(object entity, string propertyName)
    {
        if (!_propertyAccessors.TryGetValue(propertyName, out Func<object, object?>? accessor))
            throw new KeyNotFoundException($"The property {propertyName} does not exist on the type {_dataType.Name}");

        return accessor(entity);
    }

    /// <inheritdoc />
    public ExpandoObject ShapeData(object entity, IEnumerable<string> propertyNames)
    {
        ExpandoObject shaped = new();
        foreach (string propertyName in propertyNames)
            shaped.TryAdd(propertyName, GetValue(entity, propertyName));
        return shaped;
    }

    private void BuildPropertyAccessors(JsonNamingPolicy nameConverter)
    {
        foreach (PropertyInfo property in typeof(T).GetProperties())
        {
            ParameterExpression parameterExpr = Expression.Parameter(_dataType, nameof(T));
            MemberExpression propertyExpr = Expression.Property(parameterExpr, property);

            Expression<Func<T, object?>> lambda;

            if (property.PropertyType.IsPrimitive)
            {
                // Primitives are not covariant with object in a Func definition
                // so we must explicitly convert them
                Type funcType = typeof(Func<,>).MakeGenericType(_dataType, property.PropertyType);
                LambdaExpression primitiveLambda = Expression.Lambda(funcType, propertyExpr, parameterExpr);
                Expression converted = Expression.Convert(primitiveLambda.Body, typeof(object));
                lambda = Expression.Lambda<Func<T,object?>>(converted, primitiveLambda.Parameters);
            }
            else
            {
                lambda = Expression.Lambda<Func<T, object?>>(propertyExpr, parameterExpr);
            }

            string convertedName = nameConverter.ConvertName(property.Name);
            _propertyAccessors[convertedName] = Unsafe.As<Func<object, object?>>(lambda.Compile());
        }
    }
}
