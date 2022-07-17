using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

#pragma warning disable CS1591

namespace Sanctuary.Census.Benchmarks;

public class ObjectShaperBenchmarks
{
    private readonly MyRecord _record;
    private readonly ExpressionObjectShaper<MyRecord> _expressionShaper;
    private readonly ReflectionObjectShaper<MyRecord> _reflectionObjectShaper;

    public ObjectShaperBenchmarks()
    {
        _record = new MyRecord(true, "x", 3);
        _expressionShaper = new ExpressionObjectShaper<MyRecord>();
        _reflectionObjectShaper = new ReflectionObjectShaper<MyRecord>();
    }

    [Benchmark]
    public (object?, object?, object?) ExpressionOS()
    {
        object? v1 = _expressionShaper.GetPropertyValue(nameof(MyRecord.Value1), _record);
        object? v2 = _expressionShaper.GetPropertyValue(nameof(MyRecord.Value2), _record);
        object? v3 = _expressionShaper.GetPropertyValue(nameof(MyRecord.Value3), _record);
        return (v1, v2, v3);
    }

    [Benchmark]
    public (object?, object?, object?) ReflectionExpressionOS()
    {
        object? v1 = _reflectionObjectShaper.GetPropertyValue(nameof(MyRecord.Value1), _record);
        object? v2 = _reflectionObjectShaper.GetPropertyValue(nameof(MyRecord.Value2), _record);
        object? v3 = _reflectionObjectShaper.GetPropertyValue(nameof(MyRecord.Value3), _record);
        return (v1, v2, v3);
    }

    [Benchmark]
    public ExpandoObject BuildWithExpressionOS()
        => _expressionShaper.BuildExpandoObject(_record);

    [Benchmark]
    public ExpandoObject BuildWithReflectionOS()
        => _reflectionObjectShaper.BuildExpandoObject(_record);

    [Benchmark]
    public string SerializeNormal()
        => JsonSerializer.Serialize(_record);

    [Benchmark]
    public string SerializeExpando()
        => JsonSerializer.Serialize(_expressionShaper.BuildExpandoObject(_record));

    private class ExpressionObjectShaper<T> where T : class
    {
        private readonly Dictionary<string, Func<object, object?>> _propertyAccessors;

        public ExpressionObjectShaper()
        {
            _propertyAccessors = new Dictionary<string, Func<object, object?>>();

            Type tType = typeof(T);
            foreach (PropertyInfo property in typeof(T).GetProperties())
            {
                ParameterExpression parameterExpr = Expression.Parameter(tType, nameof(T));
                MemberExpression propertyExpr = Expression.Property(parameterExpr, property);

                Expression<Func<T, object?>> lambda;
                if (property.PropertyType.IsPrimitive)
                {
                    Type funcType = typeof(Func<,>).MakeGenericType(typeof(T), property.PropertyType);
                    LambdaExpression primitiveLambda = Expression.Lambda(funcType, propertyExpr, parameterExpr);
                    Expression converted = Expression.Convert(primitiveLambda.Body, typeof(object));
                    lambda = Expression.Lambda<Func<T,object?>>(converted, primitiveLambda.Parameters);
                }
                else
                {
                    lambda = Expression.Lambda<Func<T, object?>>(propertyExpr, parameterExpr);
                }

                _propertyAccessors[property.Name] = Unsafe.As<Func<object, object?>>(lambda.Compile());
            }
        }

        public ExpandoObject BuildExpandoObject(object instance)
        {
            ExpandoObject ret = new();
            foreach ((string name, Func<object, object?> accessor) in _propertyAccessors)
                ret.TryAdd(name, accessor(instance));
            return ret;
        }

        public object? GetPropertyValue(string propertyName, object instance)
            => _propertyAccessors[propertyName](instance);
    }

    private class ReflectionObjectShaper<T> where T : class
    {
        private readonly Dictionary<string, PropertyInfo> _propertyAccessors;

        public ReflectionObjectShaper()
        {
            _propertyAccessors = new Dictionary<string, PropertyInfo>();
            foreach (PropertyInfo property in typeof(T).GetProperties())
                _propertyAccessors[property.Name] = property;
        }

        public object? GetPropertyValue(string propertyName, object instance)
            => _propertyAccessors[propertyName].GetValue(instance);

        public ExpandoObject BuildExpandoObject(object instance)
        {
            ExpandoObject ret = new();
            foreach (PropertyInfo accessor in _propertyAccessors.Values)
                ret.TryAdd(accessor.Name, accessor.GetValue(instance));
            return ret;
        }
    }

    private record MyRecord(bool Value1, string Value2, uint Value3);
}
