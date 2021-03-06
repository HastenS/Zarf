﻿using System.Collections.Generic;
using System.Linq.Expressions;

namespace Zarf.Extensions
{
    public static class ExpressionExtensions
    {
        public static Expression UnWrap(this Expression expression)
        {
            if (expression.NodeType == ExpressionType.Quote)
            {
                return UnWrap(((UnaryExpression)expression)?.Operand);
            }

            return expression;
        }

        public static IEnumerable<ParameterExpression> GetParameters(this Expression expression)
        {
            return expression.UnWrap().As<LambdaExpression>()?.Parameters;
        }

        public static bool IsNullValueConstant(this Expression node)
        {
            if (node.Is<ConstantExpression>())
            {
                return node.As<ConstantExpression>().Value == null;
            }

            return false;
        }

        public static T GetValue<T>(this ConstantExpression constant)
        {
            if (constant == null)
            {
                return default(T);
            }

            return constant.Value.Cast<T>();
        }
    }
}
