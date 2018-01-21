﻿using System.Linq;
using System.Linq.Expressions;
using Zarf.Extensions;
using Zarf.Queries.Expressions;

namespace Zarf.Queries.ExpressionVisitors
{
    /// <summary>
    /// 根据子查询生成Column
    /// </summary>
    public class ProjectionExpressionVisitor : QueryCompiler
    {
        public QueryExpression Query { get; }

        public ProjectionExpressionVisitor(QueryExpression query, IQueryContext queryContext)
            : base(queryContext)
        {
            Query = query;
            Query.Projections.Clear();
        }

        protected override Expression VisitMemberInit(MemberInitExpression memberInit)
        {
            VisitNew(memberInit.NewExpression);

            foreach (var binding in memberInit.Bindings.OfType<MemberAssignment>())
            {
                if (binding.Expression.Is<QueryExpression>())
                {
                    continue;
                }

                if (binding.Expression.NodeType == ExpressionType.Parameter)
                {
                    VisitParameter(binding.Expression.As<ParameterExpression>());
                    continue;
                }

                if (!ReflectionUtil.SimpleTypes.Contains(binding.Expression.Type))
                {
                    continue;
                }

                if (binding.Expression.NodeType != ExpressionType.Extension &&
                    binding.Expression.NodeType != ExpressionType.Constant)
                {
                    continue;
                }

                Query.AddProjection(binding.Expression);
                QueryContext.ProjectionOwner.AddProjection(binding.Expression, Query);
                QueryContext.MemberBindingMapper.Map(Expression.MakeMemberAccess(memberInit, binding.Member), binding.Expression);
            }

            return memberInit;
        }

        protected override Expression VisitNew(NewExpression newExpression)
        {
            for (var i = 0; i < newExpression.Arguments.Count; i++)
            {
                if (newExpression.Arguments[i].Is<QueryExpression>())
                {
                    continue;
                }

                if (newExpression.Arguments[i].NodeType == ExpressionType.Parameter)
                {
                    VisitParameter(newExpression.Arguments[i].As<ParameterExpression>());
                    continue;
                }

                if (!ReflectionUtil.SimpleTypes.Contains(newExpression.Arguments[i].Type))
                {
                    continue;
                }

                if (newExpression.Arguments[i].NodeType != ExpressionType.Extension &&
                    newExpression.Arguments[i].NodeType != ExpressionType.Constant)
                {
                    continue;
                }

                Query.AddProjection(newExpression.Arguments[i]);
                QueryContext.ProjectionOwner.AddProjection(newExpression.Arguments[i], Query);
                QueryContext.MemberBindingMapper.Map(Expression.MakeMemberAccess(newExpression, newExpression.Members[i]), newExpression.Arguments[i]);
            }

            return newExpression;
        }

        protected override Expression VisitParameter(ParameterExpression parameter)
        {
            var query = QueryContext.QueryMapper.GetMappedQuery(parameter);
            if (query == null)
            {
                return parameter;
            }

            foreach (var item in query.GenQueryProjections())
            {
                Query.AddProjection(item);
                QueryContext.ProjectionOwner.AddProjection(item, Query);
            }

            return parameter;
        }

        public override Expression Visit(Expression node)
        {
            var visitedNode = base.Visit(node);

            if (visitedNode.NodeType == ExpressionType.New)
            {
                return VisitNew(visitedNode.As<NewExpression>());
            }

            if (visitedNode.NodeType == ExpressionType.MemberInit)
            {
                return VisitMemberInit(visitedNode.As<MemberInitExpression>());
            }

            if (visitedNode is ColumnExpression)
            {
                Query.AddProjection(visitedNode);
            }

            if(visitedNode is AggregateExpression)
            {
                Query.AddProjection(visitedNode);
            }

            if(visitedNode is AliasExpression)
            {
                Query.AddProjection(visitedNode);
            }

            return visitedNode;
        }
    }
}
