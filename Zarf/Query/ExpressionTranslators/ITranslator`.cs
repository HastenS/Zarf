﻿using System.Linq.Expressions;

namespace Zarf.Query.ExpressionTranslators
{
    public interface ITranslator<in T> : ITranslaor
    {
        Expression Translate(QueryContext context, T node, ExpressionVisitor transformVisitor);
    }

    public interface ITranslaor
    {
        Expression Translate(QueryContext context, Expression expression, ExpressionVisitor transformVisitor);
    }
}
