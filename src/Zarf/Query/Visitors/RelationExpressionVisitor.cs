﻿using System.Linq.Expressions;
using Zarf.Extensions;
using Zarf.Query.Expressions;

namespace Zarf.Query.Visitors
{
    /// <summary>
    /// 条件解析
    /// Where(item=>item.Id>1)
    /// </summary>
    public class RelationExpressionVisitor : QueryExpressionVisitor
    {
        public RelationExpressionVisitor(IQueryContext context) : base(context)
        {

        }

        public override Expression Compile(Expression query)
        {
            if (query == null)
            {
                return query;
            }

            if (query.NodeType == ExpressionType.MemberAccess)
            {
                return VisitMember(query.As<MemberExpression>());
            }

            return base.Compile(query);
        }

        /// <summary>
        /// 条件语句中不需要转换为CaseWhenExpression
        /// </summary>
        /// <returns></returns>
        protected override Expression ConvertBoolMethodCallToCaseWhen(MethodCallExpression methodCall)
        {
            return methodCall;
        }

        protected override Expression VisitMember(MemberExpression mem)
        {
            var queryModel = QueryContext.ModelMapper.GetValue(mem.Expression);
            if (queryModel != null)
            {
                var property = Expression.MakeMemberAccess(queryModel.Model, mem.Member);
                var binding = QueryContext.BindingMaper.GetValue(property);
                if (binding.NodeType != ExpressionType.Extension)
                {
                    binding = base.Visit(binding);
                }

                var mappedExpression = QueryContext.ExpressionMapper.GetValue(binding);
                if (mappedExpression != null)
                {
                    binding = mappedExpression;
                }

                if (binding.Is<AliasExpression>())
                {
                    return binding.As<AliasExpression>().Expression;
                }

                return binding;
            }

            return base.Compile(mem);
        }
    }
}
