﻿using System;
using System.Linq.Expressions;

namespace Zarf.Query.Expressions
{
    public class AnyExpression : Expression
    {
        public Expression Expression { get; }

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Type Type { get; }

        public AnyExpression(Expression expression)
        {
            Type = typeof(bool);
            Expression = expression;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Expression.GetHashCode();
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return (obj is AnyExpression) && GetHashCode() == obj.GetHashCode();
        }
    }
}
