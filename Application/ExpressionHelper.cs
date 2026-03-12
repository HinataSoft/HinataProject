using System.Linq.Expressions;

namespace HinataProject.Application;

public static class ExpressionHelper
{
    public static string GetPropertyPath<TSource>(Expression<Func<TSource, object>> expression)
    {
        return GetPropertyPathInternal(expression.Body);
    }

    private static string GetPropertyPathInternal(Expression expression)
    {
        return expression switch
        {
            MemberExpression memberExpression => GetMemberPath(memberExpression),
            UnaryExpression { Operand: MemberExpression operandMember } => GetMemberPath(operandMember),
            _ => throw new ArgumentException($"Expression '{expression}' is not a valid property expression")
        };
    }

    private static string GetMemberPath(MemberExpression memberExpression)
    {
        var path = memberExpression.Member.Name;
        
        if (memberExpression.Expression is MemberExpression parentMember)
        {
            path = GetMemberPath(parentMember) + "." + path;
        }
        
        return path;
    }
}