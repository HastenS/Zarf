﻿using System.Linq.Expressions;

namespace Zarf.Query.Expressions
{
    public class SourceExpression : Expression
    {
        public Expression Source { get; }

        public override ExpressionType NodeType => ExpressionType.Extension;

        public SourceExpression(Expression source)
        {
            Source = source;
        }
    }
}
