using System.Linq.Expressions;
using System.Reflection;

namespace HinataProject.Persistence.Utils;

public static class QueryableExtensions
{
    public static IOrderedQueryable<T> OrderByProperty<T>(this IQueryable<T> source, string propertyPath,
        bool isDescending)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = GetPropertyExpression(parameter, propertyPath);

        if (property is null)
            throw new ArgumentException($"Invalid property path: {propertyPath}", nameof(propertyPath));

        var lambda = Expression.Lambda(Expression.Convert(property, typeof(object)), parameter);

        var orderByMethod = isDescending
            ? typeof(Queryable).GetMethods().First(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 2)
            : typeof(Queryable).GetMethods().First(m => m.Name == "OrderBy" && m.GetParameters().Length == 2);

        var genericMethod = orderByMethod.MakeGenericMethod(typeof(T), typeof(object));
        
        return (IOrderedQueryable<T>)genericMethod.Invoke(null, new object[] { source, lambda })!;
    }

    private static Expression? GetPropertyExpression(Expression parameter, string propertyPath)
    {
        var properties = propertyPath.Split('.');
        var expression = parameter;

        foreach (var prop in properties)
        {
            var property = expression.Type.GetProperty(prop,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property is null)
                return null;

            expression = Expression.Property(expression, property);
        }

        return expression;
    }

    public static (string Field, bool IsDescending) ParseSortParameter(string sort)
    {
        if (sort is null or not { Length: > 0 })
            throw new ArgumentException(sort);

        var parts = sort.Split(new[] { ':', ' ' },
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var field = parts[0];

        if (parts.Length is 1)
            return (field, true);
        
        if (parts.Length > 2)
            throw new ArgumentOutOfRangeException(nameof(sort), "sort parameter cannot contain 3 parts");

        var sortingOrderText = parts[1].ToLower();

        if (sortingOrderText is not "asc" and not "desc")
            throw new ArgumentOutOfRangeException(nameof(sort), $"Sort order must be specified as asc or desc, found {sortingOrderText}");
        
        return (field, sortingOrderText is "desc");
    }
}
